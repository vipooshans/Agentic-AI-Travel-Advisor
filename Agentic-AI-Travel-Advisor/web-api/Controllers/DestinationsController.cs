using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelAdvisor.Core.DTOs.Destinations;
using TravelAdvisor.Core.Entities;
using TravelAdvisor.Infrastructure.Data;

namespace TravelAdvisor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DestinationsController : ControllerBase
{
    private readonly AppDbContext _context;

    public DestinationsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<DestinationDto>>> GetAll()
    {
        var destinations = await _context.Destinations
            .OrderBy(d => d.Name)
            .Select(d => new DestinationDto
            {
                Id = d.Id,
                Name = d.Name,
                Country = d.Country,
                Description = d.Description,
                ImageUrl = d.ImageUrl
            })
            .ToListAsync();

        return Ok(destinations);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DestinationDetailDto>> GetById(int id)
    {
        var destination = await _context.Destinations
            .Where(d => d.Id == id)
            .Select(d => new DestinationDetailDto
            {
                Id = d.Id,
                Name = d.Name,
                Country = d.Country,
                Description = d.Description,
                ImageUrl = d.ImageUrl,
                PackageCount = d.TravelPackages.Count
            })
            .FirstOrDefaultAsync();

        if (destination is null)
            return NotFound(new { message = "Destination not found." });

        return Ok(destination);
    }

    [Authorize(Policy = "RequireAdmin")]
    [HttpPost]
    public async Task<ActionResult<DestinationDto>> Create([FromBody] SaveDestinationRequest request)
    {
        var error = Validate(request);
        if (error is not null)
            return BadRequest(new { message = error });

        var destination = new Destination();
        Apply(destination, request);
        _context.Destinations.Add(destination);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = destination.Id }, Map(destination));
    }

    [Authorize(Policy = "RequireAdmin")]
    [HttpPut("{id}")]
    public async Task<ActionResult<DestinationDto>> Update(int id, [FromBody] SaveDestinationRequest request)
    {
        var error = Validate(request);
        if (error is not null)
            return BadRequest(new { message = error });

        var destination = await _context.Destinations.FirstOrDefaultAsync(d => d.Id == id);
        if (destination is null)
            return NotFound(new { message = "Destination not found." });

        Apply(destination, request);
        await _context.SaveChangesAsync();
        return Ok(Map(destination));
    }

    private static string? Validate(SaveDestinationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return "Name is required.";
        if (string.IsNullOrWhiteSpace(request.Country))
            return "Country is required.";

        var imageUrl = request.ImageUrl?.Trim();
        if (string.IsNullOrEmpty(imageUrl))
            return null;

        if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri) || uri.Scheme is not "http" and not "https")
            return "Image URL must be an http or https address.";

        return null;
    }

    private static void Apply(Destination destination, SaveDestinationRequest request)
    {
        destination.Name = request.Name.Trim();
        destination.Country = request.Country.Trim();
        destination.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        destination.ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim();
    }

    private static DestinationDto Map(Destination destination) => new()
    {
        Id = destination.Id,
        Name = destination.Name,
        Country = destination.Country,
        Description = destination.Description,
        ImageUrl = destination.ImageUrl
    };
}
