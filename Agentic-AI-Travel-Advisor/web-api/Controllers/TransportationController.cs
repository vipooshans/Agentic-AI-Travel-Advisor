using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelAdvisor.Api.Infrastructure;
using TravelAdvisor.Core.DTOs.Transportation;
using TravelAdvisor.Core.Interfaces.Services;
using TravelAdvisor.Infrastructure;

namespace TravelAdvisor.Api.Controllers;

[ApiController]
[Route("api/transportation")]
public class TransportationController(ITransportationService transportationService) : ControllerBase
{
    /// <summary>Active transport options, cheapest first. Public.</summary>
    [HttpGet]
    public async Task<ActionResult<List<TransportationDto>>> Search([FromQuery] TransportationSearchQuery query, CancellationToken cancellationToken) =>
        Ok(await transportationService.SearchAsync(query, cancellationToken));

    /// <summary>Agents: own options including inactive ones. Admins: all options.</summary>
    [Authorize(Policy = AuthPolicies.TravelAgentOrAdmin)]
    [HttpGet("mine")]
    public async Task<ActionResult<List<TransportationDto>>> Mine(CancellationToken cancellationToken) =>
        Ok(await transportationService.ListMineAsync(User.ToUserContext(), cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TransportationDto>> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await transportationService.GetAsync(User.ToOptionalUserContext(), id, cancellationToken));

    [Authorize(Policy = AuthPolicies.TravelAgentOrAdmin)]
    [HttpPost]
    public async Task<ActionResult<TransportationDto>> Create([FromBody] SaveTransportationRequest request, CancellationToken cancellationToken)
    {
        var created = await transportationService.CreateAsync(User.ToUserContext(), request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [Authorize(Policy = AuthPolicies.TravelAgentOrAdmin)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<TransportationDto>> Update(int id, [FromBody] SaveTransportationRequest request, CancellationToken cancellationToken) =>
        Ok(await transportationService.UpdateAsync(User.ToUserContext(), id, request, cancellationToken));

    [Authorize(Policy = AuthPolicies.TravelAgentOrAdmin)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await transportationService.DeleteAsync(User.ToUserContext(), id, cancellationToken);
        return NoContent();
    }
}
