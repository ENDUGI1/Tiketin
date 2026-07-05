using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tiketin.Web.Contracts;
using Tiketin.Web.Domain;
using Tiketin.Web.Infrastructure;
using Tiketin.Web.Services.Interfaces;

namespace Tiketin.Web.Pages.Tickets;

public class IndexModel(ITicketService ticketService) : PageModel
{
    public PagedResponse<TicketListItem> Tickets { get; private set; } = null!;

    [BindProperty(SupportsGet = true)]
    public TicketStatus? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public int P { get; set; } = 1;

    public async Task OnGetAsync(CancellationToken ct)
    {
        // Always scoped to the caller's own tickets on this page, even for staff.
        var actor = UserContext.FromPrincipal(User) with { Roles = [] };

        Tickets = await ticketService.ListAsync(actor, new TicketListQuery
        {
            Status = Status,
            Search = Search,
            Page = Math.Max(1, P),
            PageSize = 20
        }, ct);
    }
}
