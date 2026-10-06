using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelAdvisor.Api.Infrastructure;
using TravelAdvisor.Core.DTOs.Itineraries;
using TravelAdvisor.Core.Interfaces.Services;
using TravelAdvisor.Infrastructure;

namespace TravelAdvisor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ItinerariesController(IItineraryService itineraryService) : ControllerBase
{
    [Authorize(Policy = AuthPolicies.RequireUser)]
    [HttpPost]
    public async Task<ActionResult<ItineraryDetailDto>> Create([FromBody] CreateItineraryRequest request, CancellationToken cancellationToken)
    {
        var itinerary = await itineraryService.CreateAsync(User.ToUserContext(), request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = itinerary.Id }, itinerary);
    }

    [HttpGet]
    public async Task<ActionResult<List<ItineraryDto>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await itineraryService.ListAsync(User.ToUserContext(), cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ItineraryDetailDto>> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await itineraryService.GetAsync(User.ToUserContext(), id, cancellationToken));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await itineraryService.DeleteAsync(User.ToUserContext(), id, cancellationToken);
        return NoContent();
    }
}
