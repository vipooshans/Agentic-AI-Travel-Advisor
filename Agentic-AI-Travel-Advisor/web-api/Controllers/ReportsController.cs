using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelAdvisor.Api.Infrastructure;
using TravelAdvisor.Core.DTOs.Reports;
using TravelAdvisor.Core.Interfaces.Services;
using TravelAdvisor.Infrastructure;

namespace TravelAdvisor.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController(IReportService reportService) : ControllerBase
{
    /// <summary>Booking and catalog totals scoped to the caller's role.</summary>
    [HttpGet("summary")]
    public async Task<ActionResult<ReportSummaryDto>> Summary(CancellationToken cancellationToken) =>
        Ok(await reportService.GetSummaryAsync(User.ToUserContext(), cancellationToken));

    /// <summary>Revenue, booking mix, monthly trend, ratings and top listings. Platform-wide for admins, own listings for providers.</summary>
    [Authorize(Policy = AuthPolicies.ProviderOrAdmin)]
    [HttpGet("statistics")]
    public async Task<ActionResult<StatisticsDto>> Statistics([FromQuery] StatisticsQuery query, CancellationToken cancellationToken) =>
        Ok(await reportService.GetStatisticsAsync(User.ToUserContext(), query, cancellationToken));
}
