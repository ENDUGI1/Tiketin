using Tiketin.Web.Contracts;

namespace Tiketin.Web.Services.Interfaces;

/// <summary>
/// Computes SLA state on read. Nothing is stored: breach status is always derived
/// from timestamps and the category's SLA targets, so it can never go stale.
/// </summary>
public interface ISlaService
{
    SlaComputation Compute(
        DateTimeOffset createdAt,
        DateTimeOffset? firstResponseAt,
        DateTimeOffset? resolvedAt,
        int slaResponseMinutes,
        int slaResolutionMinutes);
}
