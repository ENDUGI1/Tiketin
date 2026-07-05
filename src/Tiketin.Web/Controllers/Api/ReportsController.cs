using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tiketin.Web.Contracts;
using Tiketin.Web.Services.Interfaces;

namespace Tiketin.Web.Controllers.Api;

/// <summary>Operational reports. Admin only; all figures computed on read.</summary>
[ApiController]
[Route("api/v1/reports")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public class ReportsController(IReportService reportService) : ControllerBase
{
    /// <summary>Ticket totals per status and category, current-month volume, average rating.</summary>
    /// <response code="200">Summary returned.</response>
    /// <response code="401">Not authenticated.</response>
    /// <response code="403">Caller is not an admin.</response>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(ApiResponse<ReportSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<ReportSummaryResponse>>> Summary(CancellationToken ct)
        => Ok(new ApiResponse<ReportSummaryResponse>(await reportService.GetSummaryAsync(ct)));

    /// <summary>SLA response and resolution attainment for tickets created in the last 30 days.</summary>
    /// <response code="200">SLA report returned.</response>
    /// <response code="401">Not authenticated.</response>
    /// <response code="403">Caller is not an admin.</response>
    [HttpGet("sla")]
    [ProducesResponseType(typeof(ApiResponse<SlaReportResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<SlaReportResponse>>> Sla(CancellationToken ct)
        => Ok(new ApiResponse<SlaReportResponse>(await reportService.GetSlaReportAsync(ct)));

    /// <summary>Per-technician resolved count, active load, resolution speed, and average rating.</summary>
    /// <response code="200">Technician report returned.</response>
    /// <response code="401">Not authenticated.</response>
    /// <response code="403">Caller is not an admin.</response>
    [HttpGet("technicians")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TechnicianPerformance>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TechnicianPerformance>>>> Technicians(CancellationToken ct)
        => Ok(new ApiResponse<IReadOnlyList<TechnicianPerformance>>(await reportService.GetTechnicianReportAsync(ct)));

    /// <summary>Tickets created per day over the last 30 days, zero-filled.</summary>
    /// <response code="200">Trend series returned.</response>
    /// <response code="401">Not authenticated.</response>
    /// <response code="403">Caller is not an admin.</response>
    [HttpGet("trends")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TrendPoint>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TrendPoint>>>> Trends(CancellationToken ct)
        => Ok(new ApiResponse<IReadOnlyList<TrendPoint>>(await reportService.GetTrendsAsync(ct)));
}
