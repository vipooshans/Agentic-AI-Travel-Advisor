using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelAdvisor.Core.DTOs.Packages;
using TravelAdvisor.Core.Entities;
using TravelAdvisor.Infrastructure.Data;

namespace TravelAdvisor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PackagesController : ControllerBase
{
    private readonly AppDbContext _context;

    public PackagesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<TravelPackageDto>>> GetAll([FromQuery] int? destinationId)
    {
        var query = _context.TravelPackages
            .Include(p => p.Destination)
            .Include(p => p.Activities)
            .AsQueryable();

        if (destinationId.HasValue)
            query = query.Where(p => p.DestinationId == destinationId.Value);

        var packages = await query
            .OrderBy(p => p.Title)
            .Select(p => new TravelPackageDto
            {
                Id = p.Id,
                Title = p.Title,
                Description = p.Description,
                Price = p.Price,
                DurationDays = p.DurationDays,
                DestinationId = p.DestinationId,
                DestinationName = p.Destination.Name,
                DestinationCountry = p.Destination.Country,
                ActivityCount = p.Activities.Count
            })
            .ToListAsync();

        return Ok(packages);
    }

    [Authorize(Policy = "RequireTravelAgent")]
    [HttpGet("mine")]
    public async Task<ActionResult<List<TravelPackageDto>>> GetMine()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var packages = await _context.TravelPackages
            .Include(p => p.Destination)
            .Include(p => p.Activities)
            .Where(p => p.AgentId == userId)
            .OrderBy(p => p.Title)
            .Select(p => new TravelPackageDto
            {
                Id = p.Id,
                Title = p.Title,
                Description = p.Description,
                Price = p.Price,
                DurationDays = p.DurationDays,
                DestinationId = p.DestinationId,
                DestinationName = p.Destination.Name,
                DestinationCountry = p.Destination.Country,
                ActivityCount = p.Activities.Count
            })
            .ToListAsync();

        return Ok(packages);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TravelPackageDetailDto>> GetById(int id)
    {
        var package = await _context.TravelPackages
            .Include(p => p.Destination)
            .Include(p => p.Activities)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (package is null)
            return NotFound(new { message = "Package not found." });

        return Ok(MapToDetail(package));
    }

    [Authorize(Policy = "RequireTravelAgent")]
    [HttpPost]
    public async Task<ActionResult<TravelPackageDto>> Create([FromBody] CreatePackageRequest request)
    {
        var destinationExists = await _context.Destinations.AnyAsync(d => d.Id == request.DestinationId);
        if (!destinationExists)
            return BadRequest(new { message = "Destination not found." });

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var package = new TravelPackage
        {
            AgentId = userId,
            DestinationId = request.DestinationId,
            Title = request.Title,
            Description = request.Description,
            Price = request.Price,
            DurationDays = request.DurationDays
        };

        _context.TravelPackages.Add(package);
        await _context.SaveChangesAsync();

        var destination = await _context.Destinations.FindAsync(request.DestinationId);

        return CreatedAtAction(nameof(GetById), new { id = package.Id }, new TravelPackageDto
        {
            Id = package.Id,
            Title = package.Title,
            Description = package.Description,
            Price = package.Price,
            DurationDays = package.DurationDays,
            DestinationId = package.DestinationId,
            DestinationName = destination!.Name,
            DestinationCountry = destination.Country,
            ActivityCount = 0
        });
    }

    [Authorize(Policy = "RequireTravelAgent")]
    [HttpPut("{id}")]
    public async Task<ActionResult<TravelPackageDto>> Update(int id, [FromBody] UpdatePackageRequest request)
    {
        var package = await _context.TravelPackages
            .Include(p => p.Destination)
            .Include(p => p.Activities)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (package is null)
            return NotFound(new { message = "Package not found." });

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (package.AgentId != userId && !User.IsInRole("ADMIN"))
            return Forbid();

        var destinationExists = await _context.Destinations.AnyAsync(d => d.Id == request.DestinationId);
        if (!destinationExists)
            return BadRequest(new { message = "Destination not found." });

        package.DestinationId = request.DestinationId;
        package.Title = request.Title;
        package.Description = request.Description;
        package.Price = request.Price;
        package.DurationDays = request.DurationDays;

        await _context.SaveChangesAsync();

        var destination = await _context.Destinations.FindAsync(request.DestinationId);

        return Ok(new TravelPackageDto
        {
            Id = package.Id,
            Title = package.Title,
            Description = package.Description,
            Price = package.Price,
            DurationDays = package.DurationDays,
            DestinationId = package.DestinationId,
            DestinationName = destination!.Name,
            DestinationCountry = destination.Country,
            ActivityCount = package.Activities.Count
        });
    }

    [Authorize(Policy = "RequireTravelAgent")]
    [HttpPost("{id}/activities")]
    public async Task<ActionResult<PackageActivityDto>> AddActivity(int id, [FromBody] CreateActivityRequest request)
    {
        var package = await _context.TravelPackages.FirstOrDefaultAsync(p => p.Id == id);
        if (package is null)
            return NotFound(new { message = "Package not found." });

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (package.AgentId != userId && !User.IsInRole("ADMIN"))
            return Forbid();

        var activity = new PackageActivity
        {
            TravelPackageId = id,
            Title = request.Title,
            Description = request.Description,
            DayNumber = request.DayNumber,
            Price = request.Price,
            SortOrder = request.SortOrder
        };

        _context.PackageActivities.Add(activity);
        await _context.SaveChangesAsync();

        return Ok(new PackageActivityDto
        {
            Id = activity.Id,
            TravelPackageId = activity.TravelPackageId,
            Title = activity.Title,
            Description = activity.Description,
            DayNumber = activity.DayNumber,
            Price = activity.Price,
            SortOrder = activity.SortOrder
        });
    }

    [Authorize(Policy = "RequireTravelAgent")]
    [HttpDelete("{packageId}/activities/{activityId}")]
    public async Task<IActionResult> DeleteActivity(int packageId, int activityId)
    {
        var activity = await _context.PackageActivities
            .Include(a => a.TravelPackage)
            .FirstOrDefaultAsync(a => a.Id == activityId && a.TravelPackageId == packageId);

        if (activity is null)
            return NotFound(new { message = "Activity not found." });

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (activity.TravelPackage.AgentId != userId && !User.IsInRole("ADMIN"))
            return Forbid();

        _context.PackageActivities.Remove(activity);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private static TravelPackageDetailDto MapToDetail(TravelPackage package) => new()
    {
        Id = package.Id,
        Title = package.Title,
        Description = package.Description,
        Price = package.Price,
        DurationDays = package.DurationDays,
        DestinationId = package.DestinationId,
        DestinationName = package.Destination.Name,
        DestinationCountry = package.Destination.Country,
        ActivityCount = package.Activities.Count,
        Activities = package.Activities
            .OrderBy(a => a.DayNumber).ThenBy(a => a.SortOrder)
            .Select(a => new PackageActivityDto
            {
                Id = a.Id,
                TravelPackageId = a.TravelPackageId,
                Title = a.Title,
                Description = a.Description,
                DayNumber = a.DayNumber,
                Price = a.Price,
                SortOrder = a.SortOrder
            }).ToList()
    };
}
