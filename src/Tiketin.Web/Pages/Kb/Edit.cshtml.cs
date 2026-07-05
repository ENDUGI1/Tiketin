using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tiketin.Web.Contracts;
using Tiketin.Web.Domain;
using Tiketin.Web.Infrastructure;
using Tiketin.Web.Services.Interfaces;

namespace Tiketin.Web.Pages.Kb;

/// <summary>Create (/kb/new) and edit (/kb/edit/{id}) knowledge base articles.</summary>
[Authorize(Roles = "Technician,Admin")]
public class EditModel(IKbService kbService, ICategoryService categoryService) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public IReadOnlyList<CategoryResponse> Categories { get; private set; } = [];
    public Guid? ArticleId { get; private set; }

    public UserContext Actor => UserContext.FromPrincipal(User);

    public class InputModel
    {
        [Required(ErrorMessage = "Judul wajib diisi."), MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Isi artikel wajib diisi.")]
        public string BodyMarkdown { get; set; } = string.Empty;

        [Required(ErrorMessage = "Kategori wajib dipilih.")]
        public int? CategoryId { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(Guid? id, CancellationToken ct)
    {
        Categories = await categoryService.GetAllAsync(ct);
        ArticleId = id;

        if (id is not null)
        {
            var article = await kbService.GetByIdAsync(Actor, id.Value, ct);
            Input = new InputModel
            {
                Title = article.Title,
                BodyMarkdown = article.BodyMarkdown,
                CategoryId = article.CategoryId
            };
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid? id, CancellationToken ct)
    {
        Categories = await categoryService.GetAllAsync(ct);
        ArticleId = id;

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var request = new SaveKbArticleRequest(Input.Title, Input.BodyMarkdown, Input.CategoryId!.Value);

        try
        {
            var article = id is null
                ? await kbService.CreateAsync(Actor, request, ct)
                : await kbService.UpdateAsync(Actor, id.Value, request, ct);

            return Redirect($"/kb/{article.Slug}");
        }
        catch (DomainRuleException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
    }
}
