using Tiketin.Web.Contracts;

namespace Tiketin.Web.Services.Interfaces;

/// <summary>Lightweight user lookups for assignment dropdowns and display.</summary>
public interface IUserDirectory
{
    /// <summary>Active users holding the Technician or Admin role.</summary>
    Task<IReadOnlyList<TechnicianResponse>> GetTechniciansAsync(CancellationToken ct = default);
}
