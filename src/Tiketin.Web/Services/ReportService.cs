using Microsoft.EntityFrameworkCore;
using Tiketin.Web.Contracts;
using Tiketin.Web.Data;
using Tiketin.Web.Domain;
using Tiketin.Web.Infrastructure;
using Tiketin.Web.Services.Interfaces;

namespace Tiketin.Web.Services;

public class ReportService(AppDbContext db, ISlaService slaService, TimeProvider clock) : IReportService
{
    public async Task<ReportSummaryResponse> GetSummaryAsync(CancellationToken ct = default)
    {
        var now = clock.GetUtcNow();
        var monthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);

        var total = await db.Tickets.LongCountAsync(ct);
        var createdThisMonth = await db.Tickets.LongCountAsync(t => t.CreatedAt >= monthStart, ct);
        var resolvedThisMonth = await db.Tickets.LongCountAsync(
            t => t.ResolvedAt != null && t.ResolvedAt >= monthStart, ct);

        var averageRating = await db.Tickets
            .Where(t => t.SatisfactionRating != null)
            .Select(t => (double?)t.SatisfactionRating)
            .AverageAsync(ct);

        var byStatus = await db.Tickets
            .GroupBy(t => t.Status)
            .Select(g => new { g.Key, Count = g.LongCount() })
            .ToListAsync(ct);

        var byCategory = (await db.Tickets
                .GroupBy(t => t.Category.Name)
                .Select(g => new { g.Key, Count = g.LongCount() })
                .ToListAsync(ct))
            .OrderByDescending(c => c.Count)
            .Select(c => new CategoryCount(c.Key, c.Count))
            .ToList();

        return new ReportSummaryResponse(
            total,
            createdThisMonth,
            resolvedThisMonth,
            averageRating is null ? null : Math.Round(averageRating.Value, 2),
            Enum.GetValues<TicketStatus>()
                .Select(s => new StatusCount(
                    s.ToString(),
                    byStatus.SingleOrDefault(x => x.Key == s)?.Count ?? 0))
                .ToList(),
            byCategory);
    }

    public async Task<SlaReportResponse> GetSlaReportAsync(CancellationToken ct = default)
    {
        var since = clock.GetUtcNow().AddDays(-30);

        // Bounded window (30 days); SLA state is derived per ticket with the same
        // on-read logic the queue uses, so report and queue can never disagree.
        var tickets = await db.Tickets.AsNoTracking()
            .Where(t => t.CreatedAt >= since)
            .Select(t => new
            {
                Category = t.Category.Name,
                t.CreatedAt,
                t.FirstResponseAt,
                t.ResolvedAt,
                t.Category.SlaResponseMinutes,
                t.Category.SlaResolutionMinutes
            })
            .ToListAsync(ct);

        var perCategory = tickets
            .GroupBy(t => t.Category)
            .Select(g =>
            {
                var computed = g
                    .Select(t => slaService.Compute(
                        t.CreatedAt, t.FirstResponseAt, t.ResolvedAt,
                        t.SlaResponseMinutes, t.SlaResolutionMinutes))
                    .ToList();

                // Pending-in-window clocks are excluded: they can still go either way.
                var responseConcluded = computed.Count(c => c.Response.Result != SlaResult.Pending);
                var resolutionConcluded = computed.Count(c => c.Resolution.Result != SlaResult.Pending);
                var responseMet = computed.LongCount(c => c.Response.Result == SlaResult.Met);
                var resolutionMet = computed.LongCount(c => c.Resolution.Result == SlaResult.Met);

                return new SlaCategoryRate(
                    g.Key,
                    g.LongCount(),
                    responseMet,
                    resolutionMet,
                    Rate(responseMet, responseConcluded),
                    Rate(resolutionMet, resolutionConcluded));
            })
            .OrderBy(c => c.Category)
            .ToList();

        var totalResponseMet = perCategory.Sum(c => c.ResponseMet);
        var totalResolutionMet = perCategory.Sum(c => c.ResolutionMet);
        var allComputed = tickets
            .Select(t => slaService.Compute(
                t.CreatedAt, t.FirstResponseAt, t.ResolvedAt,
                t.SlaResponseMinutes, t.SlaResolutionMinutes))
            .ToList();

        return new SlaReportResponse(
            tickets.Count,
            Rate(totalResponseMet, allComputed.Count(c => c.Response.Result != SlaResult.Pending)),
            Rate(totalResolutionMet, allComputed.Count(c => c.Resolution.Result != SlaResult.Pending)),
            perCategory);
    }

    public async Task<IReadOnlyList<TechnicianPerformance>> GetTechnicianReportAsync(CancellationToken ct = default)
    {
        var technicians = await db.Users.AsNoTracking()
            .Where(u => db.UserRoles.Any(ur => ur.UserId == u.Id
                && db.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Technician")))
            .Select(u => new { u.Id, u.FullName })
            .ToListAsync(ct);

        var assigned = await db.Tickets.AsNoTracking()
            .Where(t => t.AssigneeId != null)
            .Select(t => new { t.AssigneeId, t.Status, t.CreatedAt, t.ResolvedAt, t.SatisfactionRating })
            .ToListAsync(ct);

        return technicians
            .Select(tech =>
            {
                var own = assigned.Where(t => t.AssigneeId == tech.Id).ToList();
                var resolvedDurations = own
                    .Where(t => t.ResolvedAt is not null)
                    .Select(t => (t.ResolvedAt!.Value - t.CreatedAt).TotalHours)
                    .ToList();
                var ratings = own
                    .Where(t => t.SatisfactionRating is not null)
                    .Select(t => (double)t.SatisfactionRating!.Value)
                    .ToList();

                return new TechnicianPerformance(
                    tech.Id,
                    tech.FullName,
                    own.LongCount(t => t.Status is TicketStatus.Resolved or TicketStatus.Closed),
                    own.LongCount(t => t.Status is TicketStatus.Open or TicketStatus.InProgress
                        or TicketStatus.Reopened),
                    resolvedDurations.Count == 0 ? null : Math.Round(resolvedDurations.Average(), 1),
                    ratings.Count == 0 ? null : Math.Round(ratings.Average(), 2));
            })
            .OrderByDescending(p => p.ResolvedCount)
            .ToList();
    }

    public async Task<IReadOnlyList<TrendPoint>> GetTrendsAsync(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var since = today.AddDays(-29);
        var sinceUtc = new DateTimeOffset(since.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        var counts = await db.Tickets.AsNoTracking()
            .Where(t => t.CreatedAt >= sinceUtc)
            .GroupBy(t => DateOnly.FromDateTime(t.CreatedAt.UtcDateTime.Date))
            .Select(g => new { Date = g.Key, Count = g.LongCount() })
            .ToListAsync(ct);

        var lookup = counts.ToDictionary(x => x.Date, x => x.Count);

        return Enumerable.Range(0, 30)
            .Select(offset => since.AddDays(offset))
            .Select(date => new TrendPoint(date, lookup.GetValueOrDefault(date)))
            .ToList();
    }

    private static double Rate(long met, long total)
        => total == 0 ? 0 : Math.Round(met * 100.0 / total, 1);
}
