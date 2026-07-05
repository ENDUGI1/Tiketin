using Microsoft.EntityFrameworkCore;
using Tiketin.Web.Contracts;
using Tiketin.Web.Data;
using Tiketin.Web.Services.Interfaces;

namespace Tiketin.Web.Services;

public class UserDirectory(AppDbContext db) : IUserDirectory
{
    public async Task<IReadOnlyList<TechnicianResponse>> GetTechniciansAsync(CancellationToken ct = default)
    {
        var staffRoleIds = await db.Roles
            .Where(r => r.Name == "Technician" || r.Name == "Admin")
            .Select(r => r.Id)
            .ToListAsync(ct);

        return await db.Users.AsNoTracking()
            .Where(u => u.IsActive)
            .Where(u => db.UserRoles.Any(ur => ur.UserId == u.Id && staffRoleIds.Contains(ur.RoleId)))
            .OrderBy(u => u.FullName)
            .Select(u => new TechnicianResponse(u.Id, u.FullName))
            .ToListAsync(ct);
    }
}
