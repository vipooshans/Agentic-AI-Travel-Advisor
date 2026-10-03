using System.Linq.Expressions;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelAdvisor.Core.DTOs.Bookings;
using TravelAdvisor.Core.Entities;
using TravelAdvisor.Core.Enums;
using TravelAdvisor.Infrastructure.Data;

namespace TravelAdvisor.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class BookingsController : ControllerBase
{
    private readonly AppDbContext _context;

    public BookingsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("mine")]
    public async Task<ActionResult<IReadOnlyList<BookingDto>>> GetMine()
    {
        var userId = GetUserId();

        var bookings = await BaseQuery()
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .Select(BookingProjection)
            .ToListAsync();

        return Ok(bookings);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BookingDto>> GetById(int id)
    {
        var userId = GetUserId();
        var booking = await BaseQuery()
            .Where(b => b.Id == id && b.UserId == userId)
            .Select(BookingProjection)
            .FirstOrDefaultAsync();

        if (booking is null)
        {
            return NotFound(new { message = "Booking not found." });
        }

        return Ok(booking);
    }

    [HttpPost]
    public async Task<ActionResult<BookingDto>> Create([FromBody] CreateBookingRequest request)
    {
        var userId = GetUserId();
        decimal totalPrice;

        if (request.RoomId.HasValue)
        {
            var room = await _context.Rooms
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == request.RoomId.Value);

            if (room is null)
            {
                return NotFound(new { message = "Room not found." });
            }

            if (!room.IsAvailable)
            {
                return Conflict(new { message = "Room is not currently available." });
            }

            var overlaps = await _context.Bookings.AnyAsync(b =>
                b.RoomId == room.Id &&
                b.Status != BookingStatus.Cancelled &&
                request.CheckIn.Date < b.CheckOut.Date &&
                request.CheckOut.Date > b.CheckIn.Date);

            if (overlaps)
            {
                return Conflict(new { message = "Room is already booked for the selected dates." });
            }

            var nights = (request.CheckOut.Date - request.CheckIn.Date).Days;
            totalPrice = room.PricePerNight * nights;
        }
        else
        {
            var package = await _context.TravelPackages
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == request.TravelPackageId!.Value);

            if (package is null)
            {
                return NotFound(new { message = "Travel package not found." });
            }

            totalPrice = package.Price;
        }

        var booking = new Booking
        {
            UserId = userId,
            RoomId = request.RoomId,
            TravelPackageId = request.TravelPackageId,
            CheckIn = request.CheckIn.Date,
            CheckOut = request.CheckOut.Date,
            Status = BookingStatus.Pending,
            TotalPrice = totalPrice,
            CreatedAt = DateTime.UtcNow
        };

        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        var created = await BaseQuery()
            .Where(b => b.Id == booking.Id)
            .Select(BookingProjection)
            .FirstAsync();

        return CreatedAtAction(nameof(GetById), new { id = booking.Id }, created);
    }

    [HttpPatch("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        var userId = GetUserId();
        var booking = await _context.Bookings
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

        if (booking is null)
        {
            return NotFound(new { message = "Booking not found." });
        }

        if (booking.Status is BookingStatus.Completed or BookingStatus.Cancelled)
        {
            return Conflict(new { message = "This booking cannot be cancelled." });
        }

        booking.Status = BookingStatus.Cancelled;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private IQueryable<Booking> BaseQuery() => _context.Bookings
        .AsNoTracking()
        .Include(b => b.Room)
            .ThenInclude(r => r!.Hotel)
        .Include(b => b.TravelPackage);

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("Authenticated user id is missing.");

    private static readonly Expression<Func<Booking, BookingDto>> BookingProjection = booking => new BookingDto
    {
        Id = booking.Id,
        RoomId = booking.RoomId,
        RoomName = booking.Room != null ? booking.Room.Name : null,
        HotelName = booking.Room != null ? booking.Room.Hotel.Name : null,
        TravelPackageId = booking.TravelPackageId,
        TravelPackageTitle = booking.TravelPackage != null ? booking.TravelPackage.Title : null,
        CheckIn = booking.CheckIn,
        CheckOut = booking.CheckOut,
        Status = booking.Status == BookingStatus.Pending ? "Pending" :
            booking.Status == BookingStatus.Confirmed ? "Confirmed" :
            booking.Status == BookingStatus.Cancelled ? "Cancelled" : "Completed",
        TotalPrice = booking.TotalPrice,
        CreatedAt = booking.CreatedAt
    };
}
