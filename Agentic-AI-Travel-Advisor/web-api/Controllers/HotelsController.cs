using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelAdvisor.Core.DTOs.Hotels;
using TravelAdvisor.Core.Entities;
using TravelAdvisor.Infrastructure.Data;

namespace TravelAdvisor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HotelsController : ControllerBase
{
    private readonly AppDbContext _context;

    public HotelsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<HotelDto>>> GetAll([FromQuery] string? city, [FromQuery] string? country)
    {
        var query = _context.Hotels.AsQueryable();

        if (!string.IsNullOrWhiteSpace(city))
            query = query.Where(h => h.City.ToLower() == city.ToLower());

        if (!string.IsNullOrWhiteSpace(country))
            query = query.Where(h => h.Country.ToLower() == country.ToLower());

        var hotels = await query
            .OrderBy(h => h.Name)
            .Select(h => new HotelDto
            {
                Id = h.Id,
                Name = h.Name,
                Address = h.Address,
                City = h.City,
                Country = h.Country,
                Description = h.Description,
                RoomCount = h.Rooms.Count
            })
            .ToListAsync();

        return Ok(hotels);
    }

    [Authorize(Policy = "RequireHotelOwner")]
    [HttpGet("mine")]
    public async Task<ActionResult<List<HotelDto>>> GetMine()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var hotels = await _context.Hotels
            .Where(h => h.OwnerId == userId)
            .OrderBy(h => h.Name)
            .Select(h => new HotelDto
            {
                Id = h.Id,
                Name = h.Name,
                Address = h.Address,
                City = h.City,
                Country = h.Country,
                Description = h.Description,
                RoomCount = h.Rooms.Count
            })
            .ToListAsync();

        return Ok(hotels);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<HotelDetailDto>> GetById(int id)
    {
        var hotel = await _context.Hotels
            .Include(h => h.Rooms)
            .FirstOrDefaultAsync(h => h.Id == id);

        if (hotel is null)
            return NotFound(new { message = "Hotel not found." });

        return Ok(MapToDetail(hotel));
    }

    [Authorize(Policy = "RequireHotelOwner")]
    [HttpPost]
    public async Task<ActionResult<HotelDto>> Create([FromBody] CreateHotelRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var hotel = new Hotel
        {
            OwnerId = userId,
            Name = request.Name,
            Address = request.Address,
            City = request.City,
            Country = request.Country,
            Description = request.Description
        };

        _context.Hotels.Add(hotel);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = hotel.Id }, new HotelDto
        {
            Id = hotel.Id,
            Name = hotel.Name,
            Address = hotel.Address,
            City = hotel.City,
            Country = hotel.Country,
            Description = hotel.Description,
            RoomCount = 0
        });
    }

    [Authorize(Policy = "RequireHotelOwner")]
    [HttpPut("{id}")]
    public async Task<ActionResult<HotelDto>> Update(int id, [FromBody] UpdateHotelRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var hotel = await _context.Hotels.FirstOrDefaultAsync(h => h.Id == id);

        if (hotel is null)
            return NotFound(new { message = "Hotel not found." });

        if (hotel.OwnerId != userId && !User.IsInRole("ADMIN"))
            return Forbid();

        hotel.Name = request.Name;
        hotel.Address = request.Address;
        hotel.City = request.City;
        hotel.Country = request.Country;
        hotel.Description = request.Description;

        await _context.SaveChangesAsync();

        return Ok(new HotelDto
        {
            Id = hotel.Id,
            Name = hotel.Name,
            Address = hotel.Address,
            City = hotel.City,
            Country = hotel.Country,
            Description = hotel.Description,
            RoomCount = await _context.Rooms.CountAsync(r => r.HotelId == hotel.Id)
        });
    }

    private static HotelDetailDto MapToDetail(Hotel hotel) => new()
    {
        Id = hotel.Id,
        Name = hotel.Name,
        Address = hotel.Address,
        City = hotel.City,
        Country = hotel.Country,
        Description = hotel.Description,
        RoomCount = hotel.Rooms.Count,
        Rooms = hotel.Rooms.Select(r => new RoomDto
        {
            Id = r.Id,
            HotelId = r.HotelId,
            Name = r.Name,
            RoomType = r.RoomType,
            PricePerNight = r.PricePerNight,
            Capacity = r.Capacity,
            IsAvailable = r.IsAvailable
        }).ToList()
    };
}
