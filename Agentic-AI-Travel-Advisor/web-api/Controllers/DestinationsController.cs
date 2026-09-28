using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelAdvisor.Core.DTOs.Destinations;
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
    public async Task<ActionResult<IReadOnlyList<DestinationDto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? country)
    {
        var query = _context.Destinations
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(d =>
                d.Name.ToLower().Contains(term) ||
                d.Country.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(country))
        {
            var countryName = country.Trim().ToLower();
            query = query.Where(d => d.Country.ToLower() == countryName);
        }

        var destinations = await query
            .OrderBy(d => d.Name)
            .Select(d => new DestinationDto
            {
                Id = d.Id,
                Name = d.Name,
                Country = d.Country,
                Description = d.Description,
                ImageUrl = d.ImageUrl,
                PackageCount = d.TravelPackages.Count
            })
            .ToListAsync();

        return Ok(destinations);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<DestinationDto>> GetById(int id)
    {
        var destination = await _context.Destinations
            .AsNoTracking()
            .Where(d => d.Id == id)
            .Select(d => new DestinationDto
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
        {
            return NotFound(new { message = "Destination not found." });
        }

        return Ok(destination);
    }
}
