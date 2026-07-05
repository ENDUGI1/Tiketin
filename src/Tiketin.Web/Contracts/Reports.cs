namespace Tiketin.Web.Contracts;

public record StatusCount(string Status, long Count);

public record CategoryCount(string Category, long Count);

public record ReportSummaryResponse(
    long TotalTickets,
    long CreatedThisMonth,
    long ResolvedThisMonth,
    double? AverageRating,
    IReadOnlyList<StatusCount> ByStatus,
    IReadOnlyList<CategoryCount> ByCategory);

public record SlaCategoryRate(
    string Category,
    long Measured,
    long ResponseMet,
    long ResolutionMet,
    double ResponseRate,
    double ResolutionRate);

/// <summary>
/// SLA attainment over the last 30 days, computed on read. A ticket counts toward
/// a clock once that clock has concluded (completed or past its deadline).
/// </summary>
public record SlaReportResponse(
    long MeasuredTickets,
    double ResponseRate,
    double ResolutionRate,
    IReadOnlyList<SlaCategoryRate> ByCategory);

public record TechnicianPerformance(
    Guid TechnicianId,
    string TechnicianName,
    long ResolvedCount,
    long ActiveCount,
    double? AverageResolutionHours,
    double? AverageRating);

public record TrendPoint(DateOnly Date, long Count);
