using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TravelAdvisor.Api.Infrastructure;
using TravelAdvisor.Core.Common;
using TravelAdvisor.Core.DTOs.Bookings;
using TravelAdvisor.Core.Interfaces.Services;
using TravelAdvisor.Infrastructure;

namespace TravelAdvisor.Api.Controllers;

[ApiController]
[Route("api/booking-agent")]
[Authorize]
public class BookingAgentController : ControllerBase
{
    private readonly IBookingAgentService _agentService;
    private readonly ISystemSettingsService _settingsService;

    public BookingAgentController(IBookingAgentService agentService, ISystemSettingsService settingsService)
    {
        _agentService = agentService;
        _settingsService = settingsService;
    }

    /// <summary>
    /// Processes a natural language booking prompt (hotel/package reservation, cancellation preview, or insights).
    /// </summary>
    [Authorize(Policy = AuthPolicies.RequireUser)]
    [EnableRateLimiting(RateLimitPolicies.Ai)]
    [HttpPost("prompt")]
    public async Task<ActionResult<BookingAgentPromptResponse>> ProcessPrompt(
        [FromBody] BookingAgentPromptRequest request,
        CancellationToken cancellationToken)
    {
        if (!await _settingsService.IsAiAssistantEnabledAsync(cancellationToken))
            throw new ServiceUnavailableException("The AI assistant is currently turned off by the administrator.");

        var response = await _agentService.ProcessPromptAsync(User.ToUserContext(), request, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Confirms and persists a valid booking proposal created by the booking agent.
    /// </summary>
    [Authorize(Policy = AuthPolicies.RequireUser)]
    [HttpPost("confirm")]
    public async Task<ActionResult<BookingDto>> ConfirmProposal(
        [FromBody] ConfirmBookingAgentRequest request,
        CancellationToken cancellationToken)
    {
        var booking = await _agentService.ConfirmProposalAsync(User.ToUserContext(), request, cancellationToken);
        return CreatedAtAction("GetById", "Bookings", new { id = booking.Id }, booking);
    }

    /// <summary>
    /// Retrieves smart concierge insights, cancellation deadlines, and tips for the caller's active bookings.
    /// </summary>
    [Authorize(Policy = AuthPolicies.RequireUser)]
    [HttpGet("insights")]
    public async Task<ActionResult<List<BookingInsightDto>>> GetInsights(CancellationToken cancellationToken)
    {
        var insights = await _agentService.GetBookingInsightsAsync(User.ToUserContext(), cancellationToken);
        return Ok(insights);
    }

    /// <summary>
    /// Retrieves the traveler's active conversation history with the booking agent.
    /// </summary>
    [Authorize(Policy = AuthPolicies.RequireUser)]
    [HttpGet("history")]
    public async Task<ActionResult<BookingAgentPromptResponse>> GetHistory(CancellationToken cancellationToken)
    {
        var history = await _agentService.GetActiveConversationAsync(User.ToUserContext(), cancellationToken);
        return Ok(history);
    }
}
