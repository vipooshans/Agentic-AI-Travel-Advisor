using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelAdvisor.Api.Infrastructure;
using TravelAdvisor.Core.DTOs.Hotels;
using TravelAdvisor.Core.Interfaces.Services;
using TravelAdvisor.Infrastructure;

namespace TravelAdvisor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HotelsController(IHotelService hotelService) : ControllerBase
{
    /// <summary>Search approved hotels. Admins see every approval state and may filter by it.</summary>
    [HttpGet]
    public async Task<ActionResult<List<HotelDto>>> GetAll([FromQuery] HotelSearchQuery query, CancellationToken cancellationToken) =>
        Ok(await hotelService.SearchAsync(User.ToOptionalUserContext(), query, cancellationToken));

    [Authorize(Policy = AuthPolicies.RequireHotelOwner)]
    [HttpGet("mine")]
    public async Task<ActionResult<List<HotelDto>>> GetMine(CancellationToken cancellationToken) =>
        Ok(await hotelService.ListMineAsync(User.ToUserContext(), cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<HotelDetailDto>> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await hotelService.GetAsync(User.ToOptionalUserContext(), id, cancellationToken));

    [Authorize(Policy = AuthPolicies.RequireHotelOwner)]
    [HttpPost]
    public async Task<ActionResult<HotelDto>> Create([FromBody] CreateHotelRequest request, CancellationToken cancellationToken)
    {
        var hotel = await hotelService.CreateAsync(User.ToUserContext(), request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = hotel.Id }, hotel);
    }

    [Authorize(Policy = AuthPolicies.HotelOwnerOrAdmin)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<HotelDto>> Update(int id, [FromBody] UpdateHotelRequest request, CancellationToken cancellationToken) =>
        Ok(await hotelService.UpdateAsync(User.ToUserContext(), id, request, cancellationToken));

    [Authorize(Policy = AuthPolicies.HotelOwnerOrAdmin)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await hotelService.DeleteAsync(User.ToUserContext(), id, cancellationToken);
        return NoContent();
    }

    [Authorize(Policy = AuthPolicies.RequireAdmin)]
    [HttpPatch("{id:int}/approval")]
    public async Task<ActionResult<HotelDto>> SetApproval(int id, [FromBody] UpdateApprovalRequest request, CancellationToken cancellationToken) =>
        Ok(await hotelService.SetApprovalAsync(id, request, cancellationToken));
}
