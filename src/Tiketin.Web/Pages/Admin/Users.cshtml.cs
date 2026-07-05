using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tiketin.Web.Contracts;
using Tiketin.Web.Domain;
using Tiketin.Web.Services.Interfaces;

namespace Tiketin.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class UsersModel(IUserAdminService userAdminService) : PageModel
{
    public PagedResponse<UserListItem> Users { get; private set; } = null!;

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public int P { get; set; } = 1;

    [BindProperty]
    public CreateInput Create { get; set; } = new();

    public bool ShowCreateForm { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? SuccessMessage { get; private set; }

    public class CreateInput
    {
        [Required(ErrorMessage = "Email wajib diisi."), EmailAddress(ErrorMessage = "Format email tidak valid.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nama wajib diisi."), MaxLength(120)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Departemen wajib diisi."), MaxLength(80)]
        public string Department { get; set; } = string.Empty;

        [Required(ErrorMessage = "Role wajib dipilih.")]
        public string Role { get; set; } = "Employee";

        [Required(ErrorMessage = "Kata sandi wajib diisi."), MinLength(8, ErrorMessage = "Kata sandi minimal 8 karakter.")]
        public string Password { get; set; } = string.Empty;
    }

    public async Task OnGetAsync(CancellationToken ct)
    {
        await LoadAsync(ct);
    }

    public async Task<IActionResult> OnPostCreateAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            ShowCreateForm = true;
            await LoadAsync(ct);
            return Page();
        }

        try
        {
            var created = await userAdminService.CreateAsync(new CreateUserRequest(
                Create.Email, Create.FullName, Create.Department, Create.Role, Create.Password), ct);
            SuccessMessage = $"Pengguna {created.FullName} berhasil dibuat.";
            Create = new CreateInput();
            ModelState.Clear();
        }
        catch (DomainRuleException ex)
        {
            ShowCreateForm = true;
            ErrorMessage = ex.Message;
        }

        await LoadAsync(ct);
        return Page();
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        Users = await userAdminService.ListAsync(Search, Math.Max(1, P), 20, ct);
    }
}
