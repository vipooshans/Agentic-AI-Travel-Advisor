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

    /// <summary>
    /// Checks dates, capacity and price for a room (checkIn + checkOut) or package (checkIn) without reserving anything.
    /// Returns 200 with available=false and a reason when the booking would be refused.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("availability")]
    public async Task<ActionResult<AvailabilityQuoteDto>> CheckAvailability([FromQuery] AvailabilityQuery query, CancellationToken cancellationToken) =>
        Ok(await bookingService.CheckAvailabilityAsync(User.ToOptionalUserContext(), query, cancellationToken));

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
