using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelAdvisor.Api.Infrastructure;
using TravelAdvisor.Core.DTOs.Hotels;
using TravelAdvisor.Core.Interfaces.Services;
using TravelAdvisor.Infrastructure;

namespace TravelAdvisor.Api.Controllers;

[ApiController]
[Route("api/hotels/{hotelId:int}/rooms")]
public class RoomsController(IRoomService roomService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<RoomDto>>> GetAll(int hotelId, CancellationToken cancellationToken) =>
        Ok(await roomService.ListAsync(User.ToOptionalUserContext(), hotelId, cancellationToken));

    [Authorize(Policy = AuthPolicies.HotelOwnerOrAdmin)]
    [HttpPost]
    public async Task<ActionResult<RoomDto>> Create(int hotelId, [FromBody] CreateRoomRequest request, CancellationToken cancellationToken)
    {
        var room = await roomService.CreateAsync(User.ToUserContext(), hotelId, request, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { hotelId }, room);
    }

    [Authorize(Policy = AuthPolicies.HotelOwnerOrAdmin)]
    [HttpPut("{roomId:int}")]
    public async Task<ActionResult<RoomDto>> Update(int hotelId, int roomId, [FromBody] UpdateRoomRequest request, CancellationToken cancellationToken) =>
        Ok(await roomService.UpdateAsync(User.ToUserContext(), hotelId, roomId, request, cancellationToken));

    [Authorize(Policy = AuthPolicies.HotelOwnerOrAdmin)]
    [HttpPatch("{roomId:int}/availability")]
    public async Task<ActionResult<RoomDto>> UpdateAvailability(int hotelId, int roomId, [FromBody] UpdateRoomAvailabilityRequest request, CancellationToken cancellationToken) =>
        Ok(await roomService.SetAvailabilityAsync(User.ToUserContext(), hotelId, roomId, request.IsAvailable, cancellationToken));

    [Authorize(Policy = AuthPolicies.HotelOwnerOrAdmin)]
    [HttpDelete("{roomId:int}")]
    public async Task<IActionResult> Delete(int hotelId, int roomId, CancellationToken cancellationToken)
    {
        await roomService.DeleteAsync(User.ToUserContext(), hotelId, roomId, cancellationToken);
        return NoContent();
    }
}
