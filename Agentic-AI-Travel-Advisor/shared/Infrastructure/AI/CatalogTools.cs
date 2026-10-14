using Microsoft.EntityFrameworkCore;
using TravelAdvisor.Core.DTOs.AI;
using TravelAdvisor.Core.Enums;
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
        var destinations = _context.Destinations.AsNoTracking();

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
            .AsNoTracking()
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
            .AsNoTracking()
            .Where(r => r.IsAvailable && r.Capacity >= minCapacity && r.Hotel.ApprovalStatus == ApprovalStatus.Approved);

        if (!string.IsNullOrWhiteSpace(city))
        {
            var term = city.Trim();
            rooms = rooms.Where(r =>
                EF.Functions.ILike(r.Hotel.City, $"%{term}%") ||
                EF.Functions.ILike(r.Hotel.Name, $"%{term}%"));
        }

        if (maxPricePerNight.HasValue)
            rooms = rooms.Where(r => r.PricePerNight <= maxPricePerNight.Value);

        var matches = await rooms
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

        var hotelIds = matches.Select(m => m.HotelId).Distinct().ToList();
        var ratings = await _context.Reviews
            .AsNoTracking()
            .Where(r => r.HotelId != null && hotelIds.Contains(r.HotelId.Value) && r.Status == ReviewStatus.Visible)
            .GroupBy(r => r.HotelId!.Value)
            .Select(g => new { HotelId = g.Key, Average = g.Average(r => (double)r.Rating) })
            .ToDictionaryAsync(x => x.HotelId, x => x.Average, cancellationToken);

        foreach (var match in matches)
            match.AverageRating = ratings.TryGetValue(match.HotelId, out var avg) ? Math.Round(avg, 1) : null;

        return matches;
    }

    public async Task<List<CatalogPackageMatch>> SearchPackagesAsync(
        int? destinationId,
        string? destinationName,
        decimal? maxPrice,
        int? maxDurationDays,
        CancellationToken cancellationToken = default)
    {
        var packages = _context.TravelPackages
            .AsNoTracking()
            .Include(p => p.Destination)
            .Include(p => p.Activities)
            .Where(p => p.ApprovalStatus == ApprovalStatus.Approved);

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

        var packageIds = list.Select(p => p.Id).ToList();
        var ratings = await _context.Reviews
            .AsNoTracking()
            .Where(r => r.TravelPackageId != null && packageIds.Contains(r.TravelPackageId.Value) && r.Status == ReviewStatus.Visible)
            .GroupBy(r => r.TravelPackageId!.Value)
            .Select(g => new { PackageId = g.Key, Average = g.Average(r => (double)r.Rating) })
            .ToDictionaryAsync(x => x.PackageId, x => x.Average, cancellationToken);

        return list.Select(p => new CatalogPackageMatch
        {
            Id = p.Id,
            Title = p.Title,
            Description = p.Description,
            Price = p.Price,
            DurationDays = p.DurationDays,
            DestinationId = p.DestinationId,
            DestinationName = p.Destination.Name,
            MaxTravelers = p.MaxTravelers,
            AverageRating = ratings.TryGetValue(p.Id, out var avg) ? Math.Round(avg, 1) : null,
            Activities = p.Activities
                .OrderBy(a => a.DayNumber)
                .ThenBy(a => a.SortOrder)
                .Select(a => new CatalogActivityMatch
                {
                    Id = a.Id,
                    TravelPackageId = p.Id,
                    PackageTitle = p.Title,
                    DestinationName = p.Destination.Name,
                    Title = a.Title,
                    Description = a.Description,
                    Category = a.Category,
                    DayNumber = a.DayNumber,
                    Price = a.Price,
                    SortOrder = a.SortOrder
                })
                .ToList()
        }).ToList();
    }

    public async Task<List<CatalogActivityMatch>> SearchActivitiesAsync(
        int? destinationId,
        string? destinationName,
        string? category,
        decimal? maxPrice,
        CancellationToken cancellationToken = default)
    {
        var activities = _context.PackageActivities
            .AsNoTracking()
            .Where(a => a.TravelPackage.ApprovalStatus == ApprovalStatus.Approved);

        if (destinationId.HasValue)
            activities = activities.Where(a => a.TravelPackage.DestinationId == destinationId.Value);
        else if (!string.IsNullOrWhiteSpace(destinationName))
        {
            var term = destinationName.Trim();
            activities = activities.Where(a => EF.Functions.ILike(a.TravelPackage.Destination.Name, $"%{term}%"));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var term = category.Trim();
            activities = activities.Where(a =>
                (a.Category != null && EF.Functions.ILike(a.Category, term)) ||
                EF.Functions.ILike(a.Title, $"%{term}%"));
        }

        if (maxPrice.HasValue)
            activities = activities.Where(a => a.Price <= maxPrice.Value);

        return await activities
            .OrderBy(a => a.TravelPackageId)
            .ThenBy(a => a.DayNumber)
            .ThenBy(a => a.SortOrder)
            .Select(a => new CatalogActivityMatch
            {
                Id = a.Id,
                TravelPackageId = a.TravelPackageId,
                PackageTitle = a.TravelPackage.Title,
                DestinationName = a.TravelPackage.Destination.Name,
                Title = a.Title,
                Description = a.Description,
                Category = a.Category,
                DayNumber = a.DayNumber,
                Price = a.Price,
                SortOrder = a.SortOrder
            })
            .Take(50)
            .ToListAsync(cancellationToken);
    }

    public async Task<CatalogHotelMatch?> GetRoomAsync(int roomId, CancellationToken cancellationToken = default) =>
        await _context.Rooms
            .AsNoTracking()
            .Where(r => r.Id == roomId && r.Hotel.ApprovalStatus == ApprovalStatus.Approved)
            .Select(r => new CatalogHotelMatch
            {
                HotelId = r.HotelId,
                HotelName = r.Hotel.Name,
                City = r.Hotel.City,
                Country = r.Hotel.Country,
                RoomId = r.Id,
                RoomName = r.Name,
                PricePerNight = r.PricePerNight,
                Capacity = r.Capacity
            })
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<CatalogPackageMatch?> GetPackageAsync(int packageId, CancellationToken cancellationToken = default) =>
        await _context.TravelPackages
            .AsNoTracking()
            .Where(p => p.Id == packageId && p.ApprovalStatus == ApprovalStatus.Approved)
            .Select(p => new CatalogPackageMatch
            {
                Id = p.Id,
                Title = p.Title,
                Price = p.Price,
                DurationDays = p.DurationDays,
                DestinationId = p.DestinationId,
                DestinationName = p.Destination.Name,
                MaxTravelers = p.MaxTravelers
            })
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<List<CatalogTransportMatch>> SearchTransportationAsync(
        int? destinationId,
        string? destinationName,
        string? from,
        TransportMode? mode,
        decimal? maxPricePerPerson,
        CancellationToken cancellationToken = default)
    {
        var options = _context.Transportation
            .AsNoTracking()
            .Where(t => t.IsActive);

        if (destinationId.HasValue && !string.IsNullOrWhiteSpace(destinationName))
        {
            var term = destinationName.Trim();
            options = options.Where(t => t.DestinationId == destinationId.Value || EF.Functions.ILike(t.ToLocation, $"%{term}%"));
        }
        else if (destinationId.HasValue)
            options = options.Where(t => t.DestinationId == destinationId.Value);
        else if (!string.IsNullOrWhiteSpace(destinationName))
        {
            var term = destinationName.Trim();
            options = options.Where(t => EF.Functions.ILike(t.ToLocation, $"%{term}%"));
        }

        if (!string.IsNullOrWhiteSpace(from))
        {
            var term = from.Trim();
            options = options.Where(t => EF.Functions.ILike(t.FromLocation, $"%{term}%"));
        }

        if (mode.HasValue)
            options = options.Where(t => t.Mode == mode.Value);

        if (maxPricePerPerson.HasValue)
            options = options.Where(t => t.PricePerPerson <= maxPricePerPerson.Value);

        return await options
            .OrderBy(t => t.PricePerPerson)
            .Select(t => new CatalogTransportMatch
            {
                Id = t.Id,
                Mode = t.Mode,
                FromLocation = t.FromLocation,
                ToLocation = t.ToLocation,
                DestinationId = t.DestinationId,
                TravelPackageId = t.TravelPackageId,
                DepartureTime = t.DepartureTime,
                DurationMinutes = t.DurationMinutes,
                PricePerPerson = t.PricePerPerson,
                Capacity = t.Capacity,
                Description = t.Description
            })
            .Take(25)
            .ToListAsync(cancellationToken);
    }
}
