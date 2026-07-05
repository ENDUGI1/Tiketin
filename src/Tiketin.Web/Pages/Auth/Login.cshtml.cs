using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tiketin.Web.Domain;

namespace Tiketin.Web.Pages.Auth;

[AllowAnonymous]
public class LoginModel(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public string? ErrorMessage { get; private set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Email wajib diisi.")]
        [EmailAddress(ErrorMessage = "Format email tidak valid.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Kata sandi wajib diisi.")]
        public string Password { get; set; } = string.Empty;
    }

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToPage("/Index");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await userManager.FindByEmailAsync(Input.Email);
        if (user is null || !user.IsActive)
        {
            ErrorMessage = "Email atau kata sandi salah.";
            return Page();
        }

        var result = await signInManager.PasswordSignInAsync(
            user, Input.Password, isPersistent: true, lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            ErrorMessage = result.IsLockedOut
                ? "Akun terkunci sementara karena terlalu banyak percobaan. Coba lagi beberapa menit."
                : "Email atau kata sandi salah.";
            return Page();
        }

        return LocalRedirect(string.IsNullOrEmpty(ReturnUrl) ? "/" : ReturnUrl);
    }
}
