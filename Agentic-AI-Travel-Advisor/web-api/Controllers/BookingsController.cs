using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelAdvisor.Core.DTOs.Bookings;
using TravelAdvisor.Core.Entities;
using TravelAdvisor.Core.Enums;
using TravelAdvisor.Infrastructure.Data;
using TravelAdvisor.Infrastructure.Helpers;

namespace TravelAdvisor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BookingsController : ControllerBase
{
    private readonly AppDbContext _context;

    public BookingsController(AppDbContext context)
    {
        _context = context;
    }

    [Authorize(Policy = "RequireUser")]
    [HttpPost]
    public async Task<ActionResult<BookingDto>> Create([FromBody] CreateBookingRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var (valid, error, totalPrice, checkOut) = await BookingHelper.ValidateAndPriceAsync(
            _context, request.RoomId, request.TravelPackageId, request.CheckIn, request.CheckOut);

        if (!valid)
            return BadRequest(new { message = error });

        var booking = new Booking
        {
            UserId = userId,
            RoomId = request.RoomId,
            TravelPackageId = request.TravelPackageId,
            CheckIn = request.CheckIn,
            CheckOut = checkOut,
            Status = BookingStatus.Pending,
            TotalPrice = totalPrice,
            CreatedAt = DateTime.UtcNow
        };

        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = booking.Id }, await MapToDtoAsync(booking.Id));
    }

    [HttpGet]
    public async Task<ActionResult<List<BookingDto>>> GetAll()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var role = User.FindFirstValue(ClaimTypes.Role);

        IQueryable<Booking> query = _context.Bookings
            .Include(b => b.Room).ThenInclude(r => r!.Hotel)
            .Include(b => b.TravelPackage)
            .Include(b => b.User);

        query = role switch
        {
            RoleNames.Admin => query,
            RoleNames.User => query.Where(b => b.UserId == userId),
            RoleNames.HotelOwner => query.Where(b => b.Room != null && b.Room.Hotel.OwnerId == userId),
            RoleNames.TravelAgent => query.Where(b => b.TravelPackage != null && b.TravelPackage.AgentId == userId),
            _ => query.Where(b => b.UserId == userId)
        };

        var bookings = await query
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new BookingDto
            {
                Id = b.Id,
                UserId = b.UserId,
                RoomId = b.RoomId,
                TravelPackageId = b.TravelPackageId,
                CheckIn = b.CheckIn,
                CheckOut = b.CheckOut,
                Status = b.Status,
                TotalPrice = b.TotalPrice,
                CreatedAt = b.CreatedAt,
                RoomName = b.Room != null ? b.Room.Name : null,
                HotelName = b.Room != null ? b.Room.Hotel.Name : null,
                PackageTitle = b.TravelPackage != null ? b.TravelPackage.Title : null,
                UserEmail = b.User.Email
            })
            .ToListAsync();

        return Ok(bookings);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BookingDto>> GetById(int id)
    {
        var booking = await _context.Bookings
            .Include(b => b.Room).ThenInclude(r => r!.Hotel)
            .Include(b => b.TravelPackage)
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking is null)
            return NotFound(new { message = "Booking not found." });

        if (!CanAccessBooking(booking))
            return Forbid();

        return Ok(new BookingDto
        {
            Id = booking.Id,
            UserId = booking.UserId,
            RoomId = booking.RoomId,
            TravelPackageId = booking.TravelPackageId,
            CheckIn = booking.CheckIn,
            CheckOut = booking.CheckOut,
            Status = booking.Status,
            TotalPrice = booking.TotalPrice,
            CreatedAt = booking.CreatedAt,
            RoomName = booking.Room?.Name,
            HotelName = booking.Room?.Hotel.Name,
            PackageTitle = booking.TravelPackage?.Title,
            UserEmail = booking.User.Email
        });
    }

    [HttpPatch("{id}/status")]
    public async Task<ActionResult<BookingDto>> UpdateStatus(int id, [FromBody] UpdateBookingStatusRequest request)
    {
        var booking = await _context.Bookings
            .Include(b => b.Room).ThenInclude(r => r!.Hotel)
            .Include(b => b.TravelPackage)
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking is null)
            return NotFound(new { message = "Booking not found." });

        if (!CanAccessBooking(booking))
            return Forbid();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        var isGuest = booking.UserId == userId && role == RoleNames.User;
        var canManage = BookingStatusHelper.CanManageListing(booking, userId, role);

        if (!isGuest && !canManage && role != RoleNames.Admin)
            return Forbid();

        if (role == RoleNames.HotelOwner && booking.Room is null)
            return Forbid();
        if (role == RoleNames.TravelAgent && booking.TravelPackage is null)
            return Forbid();

        if (!BookingStatusHelper.CanTransition(booking.Status, request.Status, role, isGuest))
            return BadRequest(new { message = $"Cannot change status from {booking.Status} to {request.Status}." });

        booking.Status = request.Status;
        await _context.SaveChangesAsync();
        return Ok(await MapToDtoAsync(booking.Id));
    }

    private bool CanAccessBooking(Booking booking)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var role = User.FindFirstValue(ClaimTypes.Role);

        return role switch
        {
            RoleNames.Admin => true,
            RoleNames.User => booking.UserId == userId,
            RoleNames.HotelOwner => booking.Room?.Hotel.OwnerId == userId,
            RoleNames.TravelAgent => booking.TravelPackage?.AgentId == userId,
            _ => booking.UserId == userId
        };
    }

    private async Task<BookingDto> MapToDtoAsync(int id)
    {
        var booking = await _context.Bookings
            .Include(b => b.Room).ThenInclude(r => r!.Hotel)
            .Include(b => b.TravelPackage)
            .Include(b => b.User)
            .FirstAsync(b => b.Id == id);

        return new BookingDto
        {
            Id = booking.Id,
            UserId = booking.UserId,
            RoomId = booking.RoomId,
            TravelPackageId = booking.TravelPackageId,
            CheckIn = booking.CheckIn,
            CheckOut = booking.CheckOut,
            Status = booking.Status,
            TotalPrice = booking.TotalPrice,
            CreatedAt = booking.CreatedAt,
            RoomName = booking.Room?.Name,
            HotelName = booking.Room?.Hotel.Name,
            PackageTitle = booking.TravelPackage?.Title,
            UserEmail = booking.User.Email
        };
    }
}
