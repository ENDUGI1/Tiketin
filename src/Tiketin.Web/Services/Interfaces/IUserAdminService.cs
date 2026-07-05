using Tiketin.Web.Contracts;

namespace Tiketin.Web.Services.Interfaces;

/// <summary>Admin-only user management (Razor Pages surface; not exposed on the REST API).</summary>
public interface IUserAdminService
{
    Task<PagedResponse<UserListItem>> ListAsync(string? search, int page, int pageSize, CancellationToken ct = default);

    Task<UserListItem> GetAsync(Guid id, CancellationToken ct = default);

    /// <exception cref="Domain.DomainRuleException">Duplicate email, unknown role, or weak password.</exception>
    Task<UserListItem> CreateAsync(CreateUserRequest request, CancellationToken ct = default);

    /// <exception cref="Domain.DomainRuleException">Would demote or deactivate the last active admin.</exception>
    Task UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken ct = default);
}
