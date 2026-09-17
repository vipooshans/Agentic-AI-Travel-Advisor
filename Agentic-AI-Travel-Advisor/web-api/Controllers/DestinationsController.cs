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
}
