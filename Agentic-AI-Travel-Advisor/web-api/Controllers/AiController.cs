using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelAdvisor.Core.DTOs.AI;
using TravelAdvisor.Core.Interfaces;

namespace TravelAdvisor.Api.Controllers;

[ApiController]
[Route("api/ai")]
[Authorize]
public class AiController : ControllerBase
{
    private readonly IAgenticAiService _ai;

    public AiController(IAgenticAiService ai)
    {
        _ai = ai;
    }

    [Authorize(Policy = "RequireUser")]
    [HttpPost("chat")]
    public async Task<ActionResult<ChatResponse>> Chat([FromBody] ChatRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { message = "Message is required." });

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        try
        {
            var response = await _ai.ChatAsync(userId, request, cancellationToken);
            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("conversations")]
    public async Task<ActionResult<List<ConversationSummaryDto>>> List(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        return Ok(await _ai.ListConversationsAsync(userId, cancellationToken));
    }

    [HttpGet("conversations/{id:int}")]
    public async Task<ActionResult<ConversationDetailDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var conversation = await _ai.GetConversationAsync(userId, id, cancellationToken);
        if (conversation is null)
            return NotFound(new { message = "Conversation not found." });
        return Ok(conversation);
    }
}
