using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelAdvisor.Api.Infrastructure;
using TravelAdvisor.Core.DTOs.Reports;
using TravelAdvisor.Core.Interfaces.Services;

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
}
