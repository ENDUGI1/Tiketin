using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tiketin.Web.Contracts;
using Tiketin.Web.Services.Interfaces;

namespace Tiketin.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class IndexModel(IReportService reportService) : PageModel
{
    public ReportSummaryResponse Summary { get; private set; } = null!;
    public SlaReportResponse Sla { get; private set; } = null!;
    public IReadOnlyList<TechnicianPerformance> Technicians { get; private set; } = [];
    public IReadOnlyList<TrendPoint> Trends { get; private set; } = [];

    public long ActiveTickets => Summary.ByStatus
        .Where(s => s.Status is "Open" or "InProgress" or "Reopened")
        .Sum(s => s.Count);

    public async Task OnGetAsync(CancellationToken ct)
    {
        Summary = await reportService.GetSummaryAsync(ct);
        Sla = await reportService.GetSlaReportAsync(ct);
        Technicians = await reportService.GetTechnicianReportAsync(ct);
        Trends = await reportService.GetTrendsAsync(ct);
    }
}
