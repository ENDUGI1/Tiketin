using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Tiketin.Web.Domain;

namespace Tiketin.Web.Infrastructure;

/// <summary>Adds profile claims to the cookie principal so pages can render them without DB hits.</summary>
public class AppClaimsPrincipalFactory(
    UserManager<AppUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    IOptions<IdentityOptions> options)
    : UserClaimsPrincipalFactory<AppUser, IdentityRole<Guid>>(userManager, roleManager, options)
{
    public const string FullNameClaim = "full_name";
    public const string DepartmentClaim = "department";

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(AppUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        identity.AddClaim(new Claim(FullNameClaim, user.FullName));
        identity.AddClaim(new Claim(DepartmentClaim, user.Department));
        return identity;
    }
}
