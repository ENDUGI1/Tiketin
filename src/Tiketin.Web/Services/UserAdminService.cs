using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Tiketin.Web.Contracts;
using Tiketin.Web.Data;
using Tiketin.Web.Domain;
using Tiketin.Web.Services.Interfaces;

namespace Tiketin.Web.Services;

public class UserAdminService(
    UserManager<AppUser> userManager,
    AppDbContext db,
    TimeProvider clock) : IUserAdminService
{
    public async Task<PagedResponse<UserListItem>> ListAsync(
        string? search, int page, int pageSize, CancellationToken ct = default)
    {
        var users = db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            users = users.Where(u =>
                EF.Functions.ILike(u.FullName, pattern) ||
                EF.Functions.ILike(u.Email!, pattern) ||
                EF.Functions.ILike(u.Department, pattern));
        }

        var total = await users.LongCountAsync(ct);

        var items = await users
            .OrderBy(u => u.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserListItem(
                u.Id,
                u.FullName,
                u.Email!,
                u.Department,
                db.UserRoles.Where(ur => ur.UserId == u.Id)
                    .Join(db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name!)
                    .FirstOrDefault() ?? "-",
                u.IsActive,
                u.CreatedAt))
            .ToListAsync(ct);

        return new PagedResponse<UserListItem>(items, PageMeta.Create(page, pageSize, total));
    }

    public async Task<UserListItem> GetAsync(Guid id, CancellationToken ct = default)
    {
        var result = await ListForIdAsync(id, ct)
            ?? throw new NotFoundException("Pengguna tidak ditemukan.");
        return result;
    }

    public async Task<UserListItem> CreateAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        EnsureKnownRole(request.Role);

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = true,
            FullName = request.FullName.Trim(),
            Department = request.Department.Trim(),
            IsActive = true,
            CreatedAt = clock.GetUtcNow()
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            throw new DomainRuleException(string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        await userManager.AddToRoleAsync(user, request.Role);
        return (await ListForIdAsync(user.Id, ct))!;
    }

    public async Task UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken ct = default)
    {
        EnsureKnownRole(request.Role);

        var user = await userManager.FindByIdAsync(id.ToString())
            ?? throw new NotFoundException("Pengguna tidak ditemukan.");

        var currentRoles = await userManager.GetRolesAsync(user);
        var currentRole = currentRoles.FirstOrDefault();

        // Never leave the system without an active admin.
        var losesAdmin = currentRole == "Admin" && (request.Role != "Admin" || !request.IsActive);
        if (losesAdmin && await CountOtherActiveAdminsAsync(user.Id, ct) == 0)
        {
            throw new DomainRuleException("Tidak bisa menonaktifkan atau menurunkan admin terakhir.");
        }

        user.FullName = request.FullName.Trim();
        user.Department = request.Department.Trim();
        user.IsActive = request.IsActive;
        await userManager.UpdateAsync(user);

        if (currentRole != request.Role)
        {
            if (currentRoles.Count > 0)
            {
                await userManager.RemoveFromRolesAsync(user, currentRoles);
            }

            await userManager.AddToRoleAsync(user, request.Role);
        }
    }

    private async Task<UserListItem?> ListForIdAsync(Guid id, CancellationToken ct)
    {
        return await db.Users.AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UserListItem(
                u.Id,
                u.FullName,
                u.Email!,
                u.Department,
                db.UserRoles.Where(ur => ur.UserId == u.Id)
                    .Join(db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name!)
                    .FirstOrDefault() ?? "-",
                u.IsActive,
                u.CreatedAt))
            .SingleOrDefaultAsync(ct);
    }

    private async Task<int> CountOtherActiveAdminsAsync(Guid excludeUserId, CancellationToken ct)
    {
        return await db.Users
            .Where(u => u.Id != excludeUserId && u.IsActive)
            .Where(u => db.UserRoles.Any(ur => ur.UserId == u.Id
                && db.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Admin")))
            .CountAsync(ct);
    }

    private static void EnsureKnownRole(string role)
    {
        if (!DbSeeder.Roles.Contains(role))
        {
            throw new DomainRuleException("Role tidak dikenal.");
        }
    }
}
