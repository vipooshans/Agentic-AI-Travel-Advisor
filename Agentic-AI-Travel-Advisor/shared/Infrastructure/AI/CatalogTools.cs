using Microsoft.EntityFrameworkCore;
using TravelAdvisor.Core.DTOs.AI;
using TravelAdvisor.Core.Interfaces;
using TravelAdvisor.Infrastructure.Data;

namespace TravelAdvisor.Infrastructure.AI;

public class CatalogTools : ICatalogTools
{
    private readonly AppDbContext _context;

    public CatalogTools(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<CatalogDestinationMatch>> SearchDestinationsAsync(
        string? query,
        CancellationToken cancellationToken = default)
    {
        var destinations = _context.Destinations.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            destinations = destinations.Where(d =>
                EF.Functions.ILike(d.Name, $"%{term}%") ||
                EF.Functions.ILike(d.Country, $"%{term}%"));
        }

        return await destinations
            .OrderBy(d => d.Name)
            .Select(d => new CatalogDestinationMatch
            {
                Id = d.Id,
                Name = d.Name,
                Country = d.Country,
                Description = d.Description
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<string>> GetDestinationNamesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Destinations
            .OrderBy(d => d.Name)
            .Select(d => d.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<CatalogHotelMatch>> SearchHotelsAsync(
        string city,
        int minCapacity,
        decimal? maxPricePerNight,
        CancellationToken cancellationToken = default)
    {
        var rooms = _context.Rooms
            .Include(r => r.Hotel)
            .Where(r => r.IsAvailable && r.Capacity >= minCapacity);

        if (!string.IsNullOrWhiteSpace(city))
        {
            var term = city.Trim();
            rooms = rooms.Where(r =>
                EF.Functions.ILike(r.Hotel.City, $"%{term}%") ||
                EF.Functions.ILike(r.Hotel.Name, $"%{term}%"));
        }

        if (maxPricePerNight.HasValue)
            rooms = rooms.Where(r => r.PricePerNight <= maxPricePerNight.Value);

        return await rooms
            .OrderBy(r => r.PricePerNight)
            .Select(r => new CatalogHotelMatch
            {
                HotelId = r.HotelId,
                HotelName = r.Hotel.Name,
                City = r.Hotel.City,
                Country = r.Hotel.Country,
                Description = r.Hotel.Description,
                RoomId = r.Id,
                RoomName = r.Name,
                PricePerNight = r.PricePerNight,
                Capacity = r.Capacity
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<CatalogPackageMatch>> SearchPackagesAsync(
        int? destinationId,
        string? destinationName,
        decimal? maxPrice,
        int? maxDurationDays,
        CancellationToken cancellationToken = default)
    {
        var packages = _context.TravelPackages
            .Include(p => p.Destination)
            .Include(p => p.Activities)
            .AsQueryable();

        if (destinationId.HasValue)
            packages = packages.Where(p => p.DestinationId == destinationId.Value);
        else if (!string.IsNullOrWhiteSpace(destinationName))
        {
            var term = destinationName.Trim();
            packages = packages.Where(p =>
                EF.Functions.ILike(p.Destination.Name, $"%{term}%") ||
                EF.Functions.ILike(p.Title, $"%{term}%"));
        }

        if (maxPrice.HasValue)
            packages = packages.Where(p => p.Price <= maxPrice.Value);

        if (maxDurationDays.HasValue)
            packages = packages.Where(p => p.DurationDays <= maxDurationDays.Value);

        var list = await packages
            .OrderBy(p => p.Price)
            .ToListAsync(cancellationToken);

        return list.Select(p => new CatalogPackageMatch
        {
            Id = p.Id,
            Title = p.Title,
            Description = p.Description,
            Price = p.Price,
            DurationDays = p.DurationDays,
            DestinationId = p.DestinationId,
            DestinationName = p.Destination.Name,
            Activities = p.Activities
                .OrderBy(a => a.DayNumber)
                .ThenBy(a => a.SortOrder)
                .Select(a => new CatalogActivityMatch
                {
                    Title = a.Title,
                    Description = a.Description,
                    DayNumber = a.DayNumber,
                    Price = a.Price,
                    SortOrder = a.SortOrder
                })
                .ToList()
        }).ToList();
    }
}
