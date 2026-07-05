using System.Security.Claims;

namespace Tiketin.Web.Infrastructure;

/// <summary>
/// Identity of the acting user, passed explicitly into services so they stay
/// independent of HttpContext and easy to unit test.
/// </summary>
public record UserContext(Guid UserId, IReadOnlyList<string> Roles)
{
    public bool IsAdmin => Roles.Contains("Admin");
    public bool IsTechnician => Roles.Contains("Technician");

    /// <summary>Technician or Admin: may see all tickets and internal notes.</summary>
    public bool IsStaff => IsAdmin || IsTechnician;

    public static UserContext FromPrincipal(ClaimsPrincipal principal)
    {
        var id = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                 ?? principal.FindFirstValue("sub")
                 ?? throw new InvalidOperationException("Authenticated principal has no user id claim.");

        var roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        return new UserContext(Guid.Parse(id), roles);
    }
}
