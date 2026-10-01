using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelAdvisor.Api.Infrastructure;
using TravelAdvisor.Core.DTOs.Bookings;
using TravelAdvisor.Core.Interfaces.Services;
using TravelAdvisor.Infrastructure;

namespace TravelAdvisor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BookingsController(IBookingService bookingService) : ControllerBase
{
    [Authorize(Policy = AuthPolicies.RequireUser)]
    [HttpPost]
    public async Task<ActionResult<BookingDto>> Create([FromBody] CreateBookingRequest request, CancellationToken cancellationToken)
    {
        var booking = await bookingService.CreateAsync(User.ToUserContext(), request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = booking.Id }, booking);
    }

    /// <summary>Guests see their own bookings, providers see bookings for their listings, admins see all.</summary>
    [HttpGet]
    public async Task<ActionResult<List<BookingDto>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await bookingService.ListAsync(User.ToUserContext(), cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BookingDto>> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await bookingService.GetAsync(User.ToUserContext(), id, cancellationToken));

    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<BookingDto>> UpdateStatus(int id, [FromBody] UpdateBookingStatusRequest request, CancellationToken cancellationToken) =>
        Ok(await bookingService.UpdateStatusAsync(User.ToUserContext(), id, request, cancellationToken));
}
