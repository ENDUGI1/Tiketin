using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Tiketin.Web.Pages.Auth;

[AllowAnonymous]
public class DeniedModel : PageModel
{
    public void OnGet()
    {
    }
}
