using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tiketin.Web.Contracts;
using Tiketin.Web.Infrastructure;
using Tiketin.Web.Services.Interfaces;

namespace Tiketin.Web.Pages.Kb;

public class ArticleModel(IKbService kbService) : PageModel
{
    public KbArticleResponse Article { get; private set; } = null!;

    public UserContext Actor => UserContext.FromPrincipal(User);

    public async Task<IActionResult> OnGetAsync(string slug, CancellationToken ct)
    {
        Article = await kbService.GetBySlugAsync(Actor, slug, ct);
        return Page();
    }

    public async Task<IActionResult> OnPostPublishAsync(string slug, bool publish, CancellationToken ct)
    {
        var article = await kbService.GetBySlugAsync(Actor, slug, ct);
        await kbService.SetPublishedAsync(Actor, article.Id, publish, ct);
        return Redirect($"/kb/{slug}");
    }
}
