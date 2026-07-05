using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tiketin.Web.Contracts;
using Tiketin.Web.Infrastructure;
using Tiketin.Web.Services.Interfaces;

namespace Tiketin.Web.Pages.Kb;

public class IndexModel(IKbService kbService, ICategoryService categoryService) : PageModel
{
    public PagedResponse<KbArticleListItem> Articles { get; private set; } = null!;
    public IReadOnlyList<CategoryResponse> Categories { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? Category { get; set; }

    [BindProperty(SupportsGet = true)]
    public int P { get; set; } = 1;

    public UserContext Actor => UserContext.FromPrincipal(User);

    public async Task OnGetAsync(CancellationToken ct)
    {
        Categories = await categoryService.GetAllAsync(ct);
        Articles = await kbService.ListAsync(Actor, new KbListQuery
        {
            Search = Search,
            Category = Category,
            Page = Math.Max(1, P),
            PageSize = 20
        }, ct);
    }
}
