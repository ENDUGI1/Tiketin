using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Tiketin.Web.Pages;

/// <summary>Role-based home redirect: Admin to dashboard, Technician to queue, Employee to own tickets.</summary>
public class IndexModel : PageModel
{
    public IActionResult OnGet()
    {
        if (User.IsInRole("Admin"))
        {
            return Redirect("/admin");
        }

        if (User.IsInRole("Technician"))
        {
            return Redirect("/queue");
        }

        return Redirect("/tickets");
    }
}
