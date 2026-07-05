using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tiketin.Web.Contracts;
using Tiketin.Web.Domain;
using Tiketin.Web.Services.Interfaces;

namespace Tiketin.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class UserEditModel(IUserAdminService userAdminService) : PageModel
{
    [BindProperty]
    public EditInput Input { get; set; } = new();

    public UserListItem Target { get; private set; } = null!;
    public string? ErrorMessage { get; private set; }

    public class EditInput
    {
        [Required(ErrorMessage = "Nama wajib diisi."), MaxLength(120)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Departemen wajib diisi."), MaxLength(80)]
        public string Department { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = "Employee";

        public bool IsActive { get; set; } = true;
    }

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken ct)
    {
        Target = await userAdminService.GetAsync(id, ct);
        Input = new EditInput
        {
            FullName = Target.FullName,
            Department = Target.Department,
            Role = Target.Role,
            IsActive = Target.IsActive
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid id, CancellationToken ct)
    {
        Target = await userAdminService.GetAsync(id, ct);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            await userAdminService.UpdateAsync(id, new UpdateUserRequest(
                Input.FullName, Input.Department, Input.Role, Input.IsActive), ct);
            return Redirect("/admin/users");
        }
        catch (DomainRuleException ex)
        {
            ErrorMessage = ex.Message;
            return Page();
        }
    }
}
