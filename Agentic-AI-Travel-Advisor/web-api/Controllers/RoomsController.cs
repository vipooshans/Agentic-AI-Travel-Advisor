using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelAdvisor.Core.DTOs.Hotels;
using TravelAdvisor.Core.Entities;
using TravelAdvisor.Infrastructure.Data;

namespace TravelAdvisor.Api.Controllers;

[ApiController]
[Route("api/hotels/{hotelId}/rooms")]
public class RoomsController : ControllerBase
{
    private readonly AppDbContext _context;

    public RoomsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<RoomDto>>> GetAll(int hotelId)
    {
        var hotel = await _context.Hotels.FirstOrDefaultAsync(h => h.Id == hotelId);
        if (hotel is null)
            return NotFound(new { message = "Hotel not found." });

        var isOwnerOrAdmin = User.Identity?.IsAuthenticated == true &&
            (User.IsInRole("ADMIN") || User.FindFirstValue(ClaimTypes.NameIdentifier) == hotel.OwnerId);
        if (hotel.ApprovalStatus != TravelAdvisor.Core.Enums.ApprovalStatus.Approved && !isOwnerOrAdmin)
            return NotFound(new { message = "Hotel not found." });

        var rooms = await _context.Rooms
            .Where(r => r.HotelId == hotelId)
            .OrderBy(r => r.Name)
            .Select(r => new RoomDto
            {
                Id = r.Id,
                HotelId = r.HotelId,
                Name = r.Name,
                RoomType = r.RoomType,
                PricePerNight = r.PricePerNight,
                Capacity = r.Capacity,
                IsAvailable = r.IsAvailable
            })
            .ToListAsync();

        return Ok(rooms);
    }

    [Authorize(Policy = "RequireHotelOwner")]
    [HttpPost]
    public async Task<ActionResult<RoomDto>> Create(int hotelId, [FromBody] CreateRoomRequest request)
    {
        var hotel = await _context.Hotels.FirstOrDefaultAsync(h => h.Id == hotelId);
        if (hotel is null)
            return NotFound(new { message = "Hotel not found." });

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (hotel.OwnerId != userId && !User.IsInRole("ADMIN"))
            return Forbid();

        var room = new Room
        {
            HotelId = hotelId,
            Name = request.Name,
            RoomType = request.RoomType,
            PricePerNight = request.PricePerNight,
            Capacity = request.Capacity,
            IsAvailable = true
        };

        _context.Rooms.Add(room);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new { hotelId }, new RoomDto
        {
            Id = room.Id,
            HotelId = room.HotelId,
            Name = room.Name,
            RoomType = room.RoomType,
            PricePerNight = room.PricePerNight,
            Capacity = room.Capacity,
            IsAvailable = room.IsAvailable
        });
    }

    [Authorize(Policy = "RequireHotelOwner")]
    [HttpPut("{roomId}")]
    public async Task<ActionResult<RoomDto>> Update(int hotelId, int roomId, [FromBody] UpdateRoomRequest request)
    {
        var room = await _context.Rooms
            .Include(r => r.Hotel)
            .FirstOrDefaultAsync(r => r.Id == roomId && r.HotelId == hotelId);

        if (room is null)
            return NotFound(new { message = "Room not found." });

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (room.Hotel.OwnerId != userId && !User.IsInRole("ADMIN"))
            return Forbid();

        room.Name = request.Name;
        room.RoomType = request.RoomType;
        room.PricePerNight = request.PricePerNight;
        room.Capacity = request.Capacity;

        await _context.SaveChangesAsync();

        return Ok(new RoomDto
        {
            Id = room.Id,
            HotelId = room.HotelId,
            Name = room.Name,
            RoomType = room.RoomType,
            PricePerNight = room.PricePerNight,
            Capacity = room.Capacity,
            IsAvailable = room.IsAvailable
        });
    }

    [Authorize(Policy = "RequireHotelOwner")]
    [HttpPatch("{roomId}/availability")]
    public async Task<ActionResult<RoomDto>> UpdateAvailability(int hotelId, int roomId, [FromBody] UpdateRoomAvailabilityRequest request)
    {
        var room = await _context.Rooms
            .Include(r => r.Hotel)
            .FirstOrDefaultAsync(r => r.Id == roomId && r.HotelId == hotelId);

        if (room is null)
            return NotFound(new { message = "Room not found." });

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (room.Hotel.OwnerId != userId && !User.IsInRole("ADMIN"))
            return Forbid();

        room.IsAvailable = request.IsAvailable;
        await _context.SaveChangesAsync();

        return Ok(new RoomDto
        {
            Id = room.Id,
            HotelId = room.HotelId,
            Name = room.Name,
            RoomType = room.RoomType,
            PricePerNight = room.PricePerNight,
            Capacity = room.Capacity,
            IsAvailable = room.IsAvailable
        });
    }
}
