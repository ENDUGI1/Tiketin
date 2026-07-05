using Microsoft.EntityFrameworkCore;
using Tiketin.Web.Contracts;
using Tiketin.Web.Data;
using Tiketin.Web.Domain;
using Tiketin.Web.Infrastructure;
using Tiketin.Web.Services.Interfaces;

namespace Tiketin.Web.Services;

public class TicketService(
    AppDbContext db,
    ITicketNumberGenerator numberGenerator,
    IFileStorage fileStorage,
    TimeProvider clock) : ITicketService
{
    public const long MaxAttachmentBytes = 5 * 1024 * 1024;

    private static readonly string[] AllowedContentTypePrefixes = ["image/"];
    private static readonly string[] AllowedContentTypes = ["application/pdf"];

    public async Task<TicketDetailResponse> CreateAsync(
        UserContext actor, CreateTicketRequest request, CancellationToken ct = default)
    {
        var category = await db.Categories.FindAsync([request.CategoryId], ct)
            ?? throw new DomainRuleException("Kategori tidak ditemukan.");

        var now = clock.GetUtcNow();

        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            TicketNumber = await numberGenerator.NextAsync(now, ct),
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            CategoryId = category.Id,
            Priority = request.Priority,
            Status = TicketStatus.Open,
            ReporterId = actor.UserId,
            CreatedAt = now
        };
        db.Tickets.Add(ticket);

        db.TicketEvents.Add(new TicketEvent
        {
            TicketId = ticket.Id,
            ActorId = actor.UserId,
            EventType = TicketEventType.Created,
            NewValue = ticket.TicketNumber,
            CreatedAt = now
        });

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return await GetAsync(actor, ticket.Id, ct);
    }

    public async Task<PagedResponse<TicketListItem>> ListAsync(
        UserContext actor, TicketListQuery query, CancellationToken ct = default)
    {
        var tickets = db.Tickets.AsNoTracking();

        if (!actor.IsStaff)
        {
            tickets = tickets.Where(t => t.ReporterId == actor.UserId);
        }

        if (query.Status is not null)
        {
            tickets = tickets.Where(t => t.Status == query.Status);
        }

        if (query.Category is not null)
        {
            tickets = tickets.Where(t => t.CategoryId == query.Category);
        }

        if (query.Priority is not null)
        {
            tickets = tickets.Where(t => t.Priority == query.Priority);
        }

        if (query.Assignee is not null)
        {
            tickets = tickets.Where(t => t.AssigneeId == query.Assignee);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search.Trim()}%";
            tickets = tickets.Where(t =>
                EF.Functions.ILike(t.TicketNumber, pattern) ||
                EF.Functions.ILike(t.Title, pattern) ||
                EF.Functions.ILike(t.Description, pattern));
        }

        var total = await tickets.LongCountAsync(ct);

        var items = await tickets
            .OrderByDescending(t => t.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(t => new TicketListItem(
                t.Id,
                t.TicketNumber,
                t.Title,
                t.Category.Name,
                t.Category.SlaResponseMinutes,
                t.Category.SlaResolutionMinutes,
                t.Priority,
                t.Status,
                t.Reporter.FullName,
                t.Assignee != null ? t.Assignee.FullName : null,
                t.CreatedAt,
                t.FirstResponseAt,
                t.ResolvedAt))
            .ToListAsync(ct);

        return new PagedResponse<TicketListItem>(items, PageMeta.Create(query.Page, query.PageSize, total));
    }

    public async Task<TicketDetailResponse> GetAsync(
        UserContext actor, Guid ticketId, CancellationToken ct = default)
    {
        var ticket = await db.Tickets.AsNoTracking()
            .Include(t => t.Category)
            .Include(t => t.Reporter)
            .Include(t => t.Assignee)
            .Include(t => t.Comments).ThenInclude(c => c.Author)
            .Include(t => t.Comments).ThenInclude(c => c.Attachments).ThenInclude(a => a.UploadedBy)
            .Include(t => t.Attachments).ThenInclude(a => a.UploadedBy)
            .SingleOrDefaultAsync(t => t.Id == ticketId, ct)
            ?? throw new NotFoundException("Tiket tidak ditemukan.");

        EnsureCanView(actor, ticket);

        var comments = ticket.Comments
            .Where(c => actor.IsStaff || !c.IsInternal)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new CommentResponse(
                c.Id,
                c.AuthorId,
                c.Author.FullName,
                c.Body,
                c.IsInternal,
                c.CreatedAt,
                c.Attachments.Select(MapAttachment).ToList()))
            .ToList();

        // Ticket-level attachments only; comment attachments render with their comment.
        var attachments = ticket.Attachments
            .Where(a => a.CommentId is null)
            .OrderBy(a => a.CreatedAt)
            .Select(MapAttachment)
            .ToList();

        return new TicketDetailResponse(
            ticket.Id,
            ticket.TicketNumber,
            ticket.Title,
            ticket.Description,
            ticket.CategoryId,
            ticket.Category.Name,
            ticket.Category.SlaResponseMinutes,
            ticket.Category.SlaResolutionMinutes,
            ticket.Priority,
            ticket.Status,
            ticket.ReporterId,
            ticket.Reporter.FullName,
            ticket.Reporter.Department,
            ticket.AssigneeId,
            ticket.Assignee?.FullName,
            ticket.FirstResponseAt,
            ticket.ResolvedAt,
            ticket.ClosedAt,
            ticket.SatisfactionRating,
            ticket.SatisfactionNote,
            ticket.CreatedAt,
            ticket.UpdatedAt,
            comments,
            attachments);
    }

    public async Task<CommentResponse> AddCommentAsync(
        UserContext actor, Guid ticketId, AddCommentRequest request, CancellationToken ct = default)
    {
        if (request.IsInternal && !actor.IsStaff)
        {
            throw new ForbiddenException("Catatan internal hanya untuk teknisi dan admin.");
        }

        var ticket = await LoadTicketAsync(ticketId, ct);
        EnsureCanView(actor, ticket);

        if (ticket.Status == TicketStatus.Closed)
        {
            throw new DomainRuleException("Tiket sudah ditutup dan tidak menerima komentar baru.");
        }

        var now = clock.GetUtcNow();

        var comment = new TicketComment
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            AuthorId = actor.UserId,
            Body = request.Body.Trim(),
            IsInternal = request.IsInternal,
            CreatedAt = now
        };
        db.TicketComments.Add(comment);

        // First visible staff response stops the SLA response clock.
        if (actor.IsStaff && !request.IsInternal && ticket.FirstResponseAt is null)
        {
            ticket.FirstResponseAt = now;
        }

        ticket.UpdatedAt = now;

        db.TicketEvents.Add(new TicketEvent
        {
            TicketId = ticket.Id,
            ActorId = actor.UserId,
            EventType = TicketEventType.Commented,
            NewValue = request.IsInternal ? "internal" : "public",
            CreatedAt = now
        });

        await db.SaveChangesAsync(ct);

        var authorName = await db.Users
            .Where(u => u.Id == actor.UserId)
            .Select(u => u.FullName)
            .SingleAsync(ct);

        return new CommentResponse(
            comment.Id, comment.AuthorId, authorName, comment.Body,
            comment.IsInternal, comment.CreatedAt, []);
    }

    public async Task<AttachmentResponse> AddAttachmentAsync(
        UserContext actor, Guid ticketId, Stream content, string fileName, string contentType, long sizeBytes,
        Guid? commentId = null, CancellationToken ct = default)
    {
        ValidateAttachment(fileName, contentType, sizeBytes);

        var ticket = await LoadTicketAsync(ticketId, ct);
        EnsureCanView(actor, ticket);

        if (ticket.Status == TicketStatus.Closed)
        {
            throw new DomainRuleException("Tiket sudah ditutup dan tidak menerima lampiran baru.");
        }

        if (commentId is not null)
        {
            var commentExists = await db.TicketComments
                .AnyAsync(c => c.Id == commentId && c.TicketId == ticketId, ct);
            if (!commentExists)
            {
                throw new DomainRuleException("Komentar tidak ditemukan pada tiket ini.");
            }
        }

        var storagePath = await fileStorage.SaveAsync(content, fileName, ct);

        var attachment = new TicketAttachment
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            CommentId = commentId,
            FileName = Path.GetFileName(fileName),
            ContentType = contentType,
            SizeBytes = sizeBytes,
            StoragePath = storagePath,
            UploadedById = actor.UserId,
            CreatedAt = clock.GetUtcNow()
        };
        db.TicketAttachments.Add(attachment);

        await db.SaveChangesAsync(ct);

        var uploaderName = await db.Users
            .Where(u => u.Id == actor.UserId)
            .Select(u => u.FullName)
            .SingleAsync(ct);

        return new AttachmentResponse(
            attachment.Id, attachment.FileName, attachment.ContentType,
            attachment.SizeBytes, uploaderName, attachment.CreatedAt);
    }

    public async Task<(Stream Content, string FileName, string ContentType)> OpenAttachmentAsync(
        UserContext actor, Guid ticketId, Guid attachmentId, CancellationToken ct = default)
    {
        var ticket = await LoadTicketAsync(ticketId, ct);
        EnsureCanView(actor, ticket);

        var attachment = await db.TicketAttachments.AsNoTracking()
            .SingleOrDefaultAsync(a => a.Id == attachmentId && a.TicketId == ticketId, ct)
            ?? throw new NotFoundException("Lampiran tidak ditemukan.");

        // Attachments on internal comments follow internal-note visibility.
        if (attachment.CommentId is not null && !actor.IsStaff)
        {
            var isInternal = await db.TicketComments
                .Where(c => c.Id == attachment.CommentId)
                .Select(c => c.IsInternal)
                .SingleAsync(ct);
            if (isInternal)
            {
                throw new ForbiddenException("Lampiran ini tidak tersedia.");
            }
        }

        return (fileStorage.OpenRead(attachment.StoragePath), attachment.FileName, attachment.ContentType);
    }

    public async Task RateAsync(
        UserContext actor, Guid ticketId, RateTicketRequest request, CancellationToken ct = default)
    {
        var ticket = await LoadTicketAsync(ticketId, ct);

        if (ticket.ReporterId != actor.UserId)
        {
            throw new ForbiddenException("Hanya pelapor yang bisa memberi penilaian.");
        }

        if (ticket.Status is not (TicketStatus.Resolved or TicketStatus.Closed))
        {
            throw new DomainRuleException("Penilaian hanya bisa diberikan setelah tiket selesai.");
        }

        if (ticket.SatisfactionRating is not null)
        {
            throw new DomainRuleException("Tiket ini sudah dinilai.");
        }

        ticket.SatisfactionRating = request.Rating;
        ticket.SatisfactionNote = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        ticket.UpdatedAt = clock.GetUtcNow();

        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<TicketEventResponse>> GetEventsAsync(
        UserContext actor, Guid ticketId, CancellationToken ct = default)
    {
        var ticket = await LoadTicketAsync(ticketId, ct);
        EnsureCanView(actor, ticket);

        return await db.TicketEvents.AsNoTracking()
            .Where(e => e.TicketId == ticketId)
            .OrderBy(e => e.CreatedAt).ThenBy(e => e.Id)
            .Select(e => new TicketEventResponse(
                e.Id,
                e.EventType,
                e.Actor != null ? e.Actor.FullName : null,
                e.OldValue,
                e.NewValue,
                e.CreatedAt))
            .ToListAsync(ct);
    }

    // ---------- helpers ----------

    private async Task<Ticket> LoadTicketAsync(Guid ticketId, CancellationToken ct)
    {
        return await db.Tickets.SingleOrDefaultAsync(t => t.Id == ticketId, ct)
            ?? throw new NotFoundException("Tiket tidak ditemukan.");
    }

    private static void EnsureCanView(UserContext actor, Ticket ticket)
    {
        if (!actor.IsStaff && ticket.ReporterId != actor.UserId)
        {
            throw new ForbiddenException("Anda tidak punya akses ke tiket ini.");
        }
    }

    private static AttachmentResponse MapAttachment(TicketAttachment a) =>
        new(a.Id, a.FileName, a.ContentType, a.SizeBytes, a.UploadedBy.FullName, a.CreatedAt);

    private static void ValidateAttachment(string fileName, string contentType, long sizeBytes)
    {
        if (sizeBytes <= 0 || sizeBytes > MaxAttachmentBytes)
        {
            throw new DomainRuleException("Ukuran lampiran maksimal 5 MB.");
        }

        var allowed = AllowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase)
                      || AllowedContentTypePrefixes.Any(p => contentType.StartsWith(p, StringComparison.OrdinalIgnoreCase));
        if (!allowed)
        {
            throw new DomainRuleException("Jenis file tidak didukung. Hanya gambar dan PDF.");
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new DomainRuleException("Nama file tidak valid.");
        }
    }
}
