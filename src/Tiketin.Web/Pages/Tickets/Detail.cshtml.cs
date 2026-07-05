using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tiketin.Web.Contracts;
using Tiketin.Web.Domain;
using Tiketin.Web.Infrastructure;
using Tiketin.Web.Services.Interfaces;

namespace Tiketin.Web.Pages.Tickets;

public class DetailModel(
    ITicketService ticketService,
    IUserDirectory userDirectory,
    TimeProvider clock) : PageModel
{
    public TicketDetailResponse Ticket { get; private set; } = null!;
    public IReadOnlyList<TicketEventResponse> Events { get; private set; } = [];
    public IReadOnlyList<TechnicianResponse> Technicians { get; private set; } = [];

    public UserContext Actor => UserContext.FromPrincipal(User);
    public bool IsReporter => Ticket.ReporterId == Actor.UserId;
    public bool CanRate => IsReporter
        && Ticket.Status is TicketStatus.Resolved or TicketStatus.Closed
        && Ticket.SatisfactionRating is null;

    public bool CanReopen => IsReporter
        && Ticket.Status == TicketStatus.Resolved
        && Ticket.ResolvedAt is not null
        && clock.GetUtcNow() - Ticket.ResolvedAt <= TimeSpan.FromDays(7);

    public IReadOnlyList<TicketStatus> AllowedStatuses =>
        Services.TicketStatusTransitionValidator.AllowedFrom(Ticket.Status)
            // Reopened is the reporter's move, not a staff transition.
            .Where(s => s != TicketStatus.Reopened)
            .ToList();

    [BindProperty]
    public CommentInput Comment { get; set; } = new();

    [BindProperty]
    public RatingInput Rating { get; set; } = new();

    public string? ErrorMessage { get; private set; }

    public class CommentInput
    {
        [Required(ErrorMessage = "Komentar tidak boleh kosong.")]
        public string Body { get; set; } = string.Empty;

        public bool IsInternal { get; set; }

        public IFormFile? Attachment { get; set; }
    }

    public class RatingInput
    {
        [Required(ErrorMessage = "Pilih nilai 1 sampai 5."), Range(1, 5)]
        public short? Value { get; set; }

        [MaxLength(300)]
        public string? Note { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken ct)
    {
        await LoadAsync(id, ct);
        return Page();
    }

    public async Task<IActionResult> OnPostCommentAsync(Guid id, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            await LoadAsync(id, ct);
            return Page();
        }

        try
        {
            var comment = await ticketService.AddCommentAsync(
                Actor, id, new AddCommentRequest(Comment.Body, Comment.IsInternal), ct);

            if (Comment.Attachment is { Length: > 0 } file)
            {
                await using var stream = file.OpenReadStream();
                await ticketService.AddAttachmentAsync(
                    Actor, id, stream, file.FileName, file.ContentType, file.Length, comment.Id, ct);
            }

            return Redirect($"/tickets/{id}");
        }
        catch (Exception ex) when (ex is DomainRuleException or ForbiddenException)
        {
            ErrorMessage = ex.Message;
            await LoadAsync(id, ct);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostRateAsync(Guid id, CancellationToken ct)
    {
        ModelState.Clear(); // only the rating fields matter on this handler

        if (Rating.Value is null or < 1 or > 5)
        {
            ErrorMessage = "Pilih nilai 1 sampai 5.";
            await LoadAsync(id, ct);
            return Page();
        }

        try
        {
            await ticketService.RateAsync(Actor, id,
                new RateTicketRequest(Rating.Value.Value, Rating.Note), ct);
            return Redirect($"/tickets/{id}");
        }
        catch (Exception ex) when (ex is DomainRuleException or ForbiddenException)
        {
            ErrorMessage = ex.Message;
            await LoadAsync(id, ct);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostStatusAsync(Guid id, TicketStatus status, CancellationToken ct)
        => await RunActionAsync(id, ct, () => ticketService.ChangeStatusAsync(Actor, id, status, ct));

    public async Task<IActionResult> OnPostAssignAsync(Guid id, Guid? assigneeId, CancellationToken ct)
        => await RunActionAsync(id, ct, () => ticketService.AssignAsync(Actor, id, assigneeId, ct));

    public async Task<IActionResult> OnPostPriorityAsync(Guid id, TicketPriority priority, CancellationToken ct)
        => await RunActionAsync(id, ct, () => ticketService.ChangePriorityAsync(Actor, id, priority, ct));

    public async Task<IActionResult> OnPostReopenAsync(Guid id, CancellationToken ct)
        => await RunActionAsync(id, ct, () => ticketService.ReopenAsync(Actor, id, ct));

    private async Task<IActionResult> RunActionAsync(Guid id, CancellationToken ct, Func<Task> action)
    {
        ModelState.Clear();

        try
        {
            await action();
            return Redirect($"/tickets/{id}");
        }
        catch (Exception ex) when (ex is DomainRuleException or ForbiddenException)
        {
            ErrorMessage = ex.Message;
            await LoadAsync(id, ct);
            return Page();
        }
    }

    private async Task LoadAsync(Guid id, CancellationToken ct)
    {
        Ticket = await ticketService.GetAsync(Actor, id, ct);
        Events = await ticketService.GetEventsAsync(Actor, id, ct);

        if (Actor.IsAdmin)
        {
            Technicians = await userDirectory.GetTechniciansAsync(ct);
        }
    }
}
