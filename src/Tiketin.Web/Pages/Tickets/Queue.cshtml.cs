using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Tiketin.Web.Pages.Tickets;

[Authorize(Roles = "Technician,Admin")]
public class QueueModel : PageModel
{
    public void OnGet()
    {
    }
}
