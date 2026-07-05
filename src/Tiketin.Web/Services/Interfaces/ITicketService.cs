using Tiketin.Web.Contracts;
using Tiketin.Web.Infrastructure;

namespace Tiketin.Web.Services.Interfaces;

public interface ITicketService
{
    /// <summary>Creates a ticket, generates its number, and records the Created event.</summary>
    Task<TicketDetailResponse> CreateAsync(UserContext actor, CreateTicketRequest request, CancellationToken ct = default);

    /// <summary>Lists tickets. Employees only ever see their own tickets.</summary>
    Task<PagedResponse<TicketListItem>> ListAsync(UserContext actor, TicketListQuery query, CancellationToken ct = default);

    /// <summary>Loads one ticket with comments and attachments. Internal notes are stripped for reporters.</summary>
    /// <exception cref="Domain.ForbiddenException">Employee requesting someone else's ticket.</exception>
    Task<TicketDetailResponse> GetAsync(UserContext actor, Guid ticketId, CancellationToken ct = default);

    /// <summary>Adds a comment. First non-internal staff comment stamps first_response_at.</summary>
    Task<CommentResponse> AddCommentAsync(UserContext actor, Guid ticketId, AddCommentRequest request, CancellationToken ct = default);

    /// <summary>Stores an uploaded file (5 MB max, images and PDF only) on the ticket.</summary>
    Task<AttachmentResponse> AddAttachmentAsync(
        UserContext actor, Guid ticketId, Stream content, string fileName, string contentType, long sizeBytes,
        Guid? commentId = null, CancellationToken ct = default);

    /// <summary>Opens an attachment for download, enforcing the same visibility rules as GetAsync.</summary>
    Task<(Stream Content, string FileName, string ContentType)> OpenAttachmentAsync(
        UserContext actor, Guid ticketId, Guid attachmentId, CancellationToken ct = default);

    /// <summary>Reporter rates a Resolved/Closed ticket, once.</summary>
    Task RateAsync(UserContext actor, Guid ticketId, RateTicketRequest request, CancellationToken ct = default);

    /// <summary>Audit trail, oldest first.</summary>
    Task<IReadOnlyList<TicketEventResponse>> GetEventsAsync(UserContext actor, Guid ticketId, CancellationToken ct = default);
}
