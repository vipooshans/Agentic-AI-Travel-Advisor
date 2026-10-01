using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelAdvisor.Core.DTOs.Destinations;
using TravelAdvisor.Core.Interfaces.Services;
using TravelAdvisor.Infrastructure;

namespace TravelAdvisor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DestinationsController(IDestinationService destinationService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<DestinationDto>>> GetAll([FromQuery] string? q, CancellationToken cancellationToken) =>
        Ok(await destinationService.ListAsync(q, cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<DestinationDetailDto>> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await destinationService.GetAsync(id, cancellationToken));

    [Authorize(Policy = AuthPolicies.RequireAdmin)]
    [HttpPost]
    public async Task<ActionResult<DestinationDto>> Create([FromBody] SaveDestinationRequest request, CancellationToken cancellationToken)
    {
        var destination = await destinationService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = destination.Id }, destination);
    }

    [Authorize(Policy = AuthPolicies.RequireAdmin)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<DestinationDto>> Update(int id, [FromBody] SaveDestinationRequest request, CancellationToken cancellationToken) =>
        Ok(await destinationService.UpdateAsync(id, request, cancellationToken));

    [Authorize(Policy = AuthPolicies.RequireAdmin)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await destinationService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
