using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelAdvisor.Core.DTOs.Itineraries;
using TravelAdvisor.Core.Entities;
using TravelAdvisor.Core.Enums;
using TravelAdvisor.Infrastructure.Data;

namespace TravelAdvisor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ItinerariesController : ControllerBase
{
    private readonly AppDbContext _context;

    public ItinerariesController(AppDbContext context)
    {
        _context = context;
    }

    [Authorize(Policy = "RequireUser")]
    [HttpPost]
    public async Task<ActionResult<ItineraryDetailDto>> Create([FromBody] CreateItineraryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(new { message = "Title is required." });
        if (request.Items.Count == 0)
            return BadRequest(new { message = "At least one itinerary item is required." });

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        if (request.DestinationId.HasValue)
        {
            var exists = await _context.Destinations.AnyAsync(d => d.Id == request.DestinationId.Value);
            if (!exists)
                return BadRequest(new { message = "Destination not found." });
        }

        var itinerary = new Itinerary
        {
            UserId = userId,
            Title = request.Title.Trim(),
            StartDate = DateTime.SpecifyKind(request.StartDate, DateTimeKind.Utc),
            EndDate = DateTime.SpecifyKind(request.EndDate, DateTimeKind.Utc),
            Status = ItineraryStatus.Draft,
            EstimatedCost = request.EstimatedCost,
            DestinationId = request.DestinationId,
            Summary = request.Summary,
            Items = request.Items.Select((item, index) => new ItineraryItem
            {
                DayNumber = item.DayNumber,
                Title = item.Title,
                Description = item.Description,
                StartTime = item.StartTime,
                SortOrder = item.SortOrder != 0 ? item.SortOrder : index
            }).ToList()
        };

        _context.Itineraries.Add(itinerary);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = itinerary.Id }, await MapDetailAsync(itinerary.Id, userId));
    }

    [HttpGet]
    public async Task<ActionResult<List<ItineraryDto>>> GetAll()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var items = await _context.Itineraries
            .Include(i => i.Destination)
            .Include(i => i.Items)
            .Where(i => i.UserId == userId)
            .OrderByDescending(i => i.StartDate)
            .Select(i => new ItineraryDto
            {
                Id = i.Id,
                Title = i.Title,
                StartDate = i.StartDate,
                EndDate = i.EndDate,
                Status = (int)i.Status,
                EstimatedCost = i.EstimatedCost,
                DestinationId = i.DestinationId,
                DestinationName = i.Destination != null ? i.Destination.Name : null,
                Summary = i.Summary,
                ItemCount = i.Items.Count
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ItineraryDetailDto>> GetById(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var dto = await MapDetailAsync(id, userId);
        if (dto is null)
            return NotFound(new { message = "Itinerary not found." });
        return Ok(dto);
    }

    private async Task<ItineraryDetailDto?> MapDetailAsync(int id, string userId)
    {
        return await _context.Itineraries
            .Include(i => i.Destination)
            .Include(i => i.Items)
            .Where(i => i.Id == id && i.UserId == userId)
            .Select(i => new ItineraryDetailDto
            {
                Id = i.Id,
                Title = i.Title,
                StartDate = i.StartDate,
                EndDate = i.EndDate,
                Status = (int)i.Status,
                EstimatedCost = i.EstimatedCost,
                DestinationId = i.DestinationId,
                DestinationName = i.Destination != null ? i.Destination.Name : null,
                Summary = i.Summary,
                ItemCount = i.Items.Count,
                Items = i.Items
                    .OrderBy(item => item.DayNumber)
                    .ThenBy(item => item.SortOrder)
                    .Select(item => new ItineraryItemDto
                    {
                        Id = item.Id,
                        DayNumber = item.DayNumber,
                        Title = item.Title,
                        Description = item.Description,
                        StartTime = item.StartTime,
                        SortOrder = item.SortOrder
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();
    }
}
