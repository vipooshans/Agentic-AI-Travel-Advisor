using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TravelAdvisor.Api.Infrastructure;
using TravelAdvisor.Core.Common;
using TravelAdvisor.Core.DTOs.AI;
using TravelAdvisor.Core.Interfaces;
using TravelAdvisor.Infrastructure;

namespace TravelAdvisor.Api.Controllers;

[ApiController]
[Route("api/ai")]
[Authorize]
public class AiController(IAgenticAiService ai) : ControllerBase
{
    [Authorize(Policy = AuthPolicies.RequireUser)]
    [EnableRateLimiting(RateLimitPolicies.Ai)]
    [HttpPost("chat")]
    public async Task<ActionResult<ChatResponse>> Chat([FromBody] ChatRequest request, CancellationToken cancellationToken)
    {
        var userId = User.ToUserContext().UserId;
        try
        {
            return Ok(await ai.ChatAsync(userId, request, cancellationToken));
        }
        catch (KeyNotFoundException ex)
        {
            throw new NotFoundException(ex.Message);
        }
        catch (ArgumentException ex)
        {
            throw new BusinessRuleException(ex.Message);
        }
    }

    [HttpGet("conversations")]
    public async Task<ActionResult<List<ConversationSummaryDto>>> List(CancellationToken cancellationToken) =>
        Ok(await ai.ListConversationsAsync(User.ToUserContext().UserId, cancellationToken));

    [HttpGet("conversations/{id:int}")]
    public async Task<ActionResult<ConversationDetailDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var conversation = await ai.GetConversationAsync(User.ToUserContext().UserId, id, cancellationToken)
                           ?? throw new NotFoundException("Conversation not found.");
        return Ok(conversation);
    }
}
