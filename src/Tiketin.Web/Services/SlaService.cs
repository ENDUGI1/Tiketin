using Tiketin.Web.Contracts;
using Tiketin.Web.Services.Interfaces;

namespace Tiketin.Web.Services;

public class SlaService(TimeProvider clock) : ISlaService
{
    public SlaComputation Compute(
        DateTimeOffset createdAt,
        DateTimeOffset? firstResponseAt,
        DateTimeOffset? resolvedAt,
        int slaResponseMinutes,
        int slaResolutionMinutes)
    {
        var now = clock.GetUtcNow();

        return new SlaComputation(
            ComputeClock(createdAt.AddMinutes(slaResponseMinutes), firstResponseAt, now),
            ComputeClock(createdAt.AddMinutes(slaResolutionMinutes), resolvedAt, now));
    }

    private static SlaClock ComputeClock(DateTimeOffset deadline, DateTimeOffset? completedAt, DateTimeOffset now)
    {
        var result = completedAt is { } done
            ? done <= deadline ? SlaResult.Met : SlaResult.Breached
            : now <= deadline ? SlaResult.Pending : SlaResult.PendingBreached;

        return new SlaClock(result, deadline);
    }
}
