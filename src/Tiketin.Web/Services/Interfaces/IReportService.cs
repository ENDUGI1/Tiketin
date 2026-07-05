using Tiketin.Web.Contracts;

namespace Tiketin.Web.Services.Interfaces;

/// <summary>Admin reporting. All figures are computed on read; nothing is precalculated.</summary>
public interface IReportService
{
    /// <summary>Totals per status and category, plus current-month volumes and average rating.</summary>
    Task<ReportSummaryResponse> GetSummaryAsync(CancellationToken ct = default);

    /// <summary>SLA response/resolution attainment for tickets created in the last 30 days.</summary>
    Task<SlaReportResponse> GetSlaReportAsync(CancellationToken ct = default);

    /// <summary>Per-technician workload, resolution speed, and satisfaction.</summary>
    Task<IReadOnlyList<TechnicianPerformance>> GetTechnicianReportAsync(CancellationToken ct = default);

    /// <summary>Tickets created per day over the last 30 days, gaps filled with zero.</summary>
    Task<IReadOnlyList<TrendPoint>> GetTrendsAsync(CancellationToken ct = default);
}
