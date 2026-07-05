using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tiketin.Web.Contracts;
using Tiketin.Web.Domain;
using Tiketin.Web.Infrastructure;
using Tiketin.Web.Services.Interfaces;

namespace Tiketin.Web.Pages.Tickets;

[Authorize(Roles = "Technician,Admin")]
public class QueueModel(
    ITicketService ticketService,
    ICategoryService categoryService,
    ISlaService slaService) : PageModel
{
    public record QueueRow(TicketListItem Ticket, SlaComputation Sla);

    public IReadOnlyList<QueueRow> Rows { get; private set; } = [];
    public PageMeta Meta { get; private set; } = null!;
    public IReadOnlyList<CategoryResponse> Categories { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public TicketStatus? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? Category { get; set; }

    [BindProperty(SupportsGet = true)]
    public TicketPriority? Priority { get; set; }

    /// <summary>"me" limits to own assignments, "none" to unassigned tickets.</summary>
    [BindProperty(SupportsGet = true)]
    public string? Assigned { get; set; }

    [BindProperty(SupportsGet = true)]
    public int P { get; set; } = 1;

    public UserContext Actor => UserContext.FromPrincipal(User);

    public async Task OnGetAsync(CancellationToken ct)
    {
        Categories = await categoryService.GetAllAsync(ct);

        var query = new TicketListQuery
        {
            Status = Status,
            Category = Category,
            Priority = Priority,
            Assignee = Assigned == "me" ? Actor.UserId : null,
            ActiveOnly = Status is null,
            QueueOrder = true,
            Page = Math.Max(1, P),
            PageSize = 25
        };

        var result = await ticketService.ListAsync(Actor, query, ct);

        var rows = result.Data.Select(t => new QueueRow(
            t, slaService.Compute(t.CreatedAt, t.FirstResponseAt, t.ResolvedAt,
                t.SlaResponseMinutes, t.SlaResolutionMinutes)));

        // "Belum ditugaskan" filter is applied in-page: the API-level filter only
        // supports a concrete assignee id.
        if (Assigned == "none")
        {
            rows = rows.Where(r => r.Ticket.AssigneeName is null);
        }

        Rows = rows.ToList();
        Meta = result.Meta;
    }

    public async Task<IActionResult> OnPostClaimAsync(Guid id, CancellationToken ct)
    {
        try
        {
            await ticketService.AssignAsync(Actor, id, Actor.UserId, ct);
        }
        catch (Exception ex) when (ex is DomainRuleException or ForbiddenException)
        {
            TempData["QueueError"] = ex.Message;
        }

        return Redirect(Request.Headers.Referer.FirstOrDefault() ?? "/queue");
    }
}
