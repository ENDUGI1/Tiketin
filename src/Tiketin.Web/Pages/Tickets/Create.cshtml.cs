using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tiketin.Web.Contracts;
using Tiketin.Web.Domain;
using Tiketin.Web.Infrastructure;
using Tiketin.Web.Services;
using Tiketin.Web.Services.Interfaces;

namespace Tiketin.Web.Pages.Tickets;

public class CreateModel(ITicketService ticketService, ICategoryService categoryService) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty]
    public List<IFormFile> Attachments { get; set; } = [];

    public IReadOnlyList<CategoryResponse> Categories { get; private set; } = [];

    public class InputModel
    {
        [Required(ErrorMessage = "Judul wajib diisi."), MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Deskripsi wajib diisi.")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Kategori wajib dipilih.")]
        public int? CategoryId { get; set; }

        [Required(ErrorMessage = "Prioritas wajib dipilih.")]
        [EnumDataType(typeof(TicketPriority))]
        public TicketPriority Priority { get; set; } = TicketPriority.Medium;
    }

    public async Task OnGetAsync(CancellationToken ct)
    {
        Categories = await categoryService.GetAllAsync(ct);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        Categories = await categoryService.GetAllAsync(ct);

        // Validate attachments up front so nothing is created when a file is invalid.
        foreach (var file in Attachments.Where(f => f.Length > 0))
        {
            if (file.Length > TicketService.MaxAttachmentBytes)
            {
                ModelState.AddModelError(string.Empty, $"Lampiran \"{file.FileName}\" melebihi 5 MB.");
            }
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var actor = UserContext.FromPrincipal(User);

        try
        {
            var ticket = await ticketService.CreateAsync(actor, new CreateTicketRequest(
                Input.Title, Input.Description, Input.CategoryId!.Value, Input.Priority), ct);

            foreach (var file in Attachments.Where(f => f.Length > 0))
            {
                await using var stream = file.OpenReadStream();
                await ticketService.AddAttachmentAsync(
                    actor, ticket.Id, stream, file.FileName, file.ContentType, file.Length, ct: ct);
            }

            return Redirect($"/tickets/{ticket.Id}");
        }
        catch (DomainRuleException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
    }
}
