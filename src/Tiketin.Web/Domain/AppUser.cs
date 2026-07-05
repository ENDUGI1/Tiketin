using Microsoft.AspNetCore.Identity;

namespace Tiketin.Web.Domain;

public class AppUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
}
