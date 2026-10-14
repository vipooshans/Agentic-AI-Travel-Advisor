using Microsoft.EntityFrameworkCore;
using TravelAdvisor.Core.Entities;
using TravelAdvisor.Core.Enums;
using TravelAdvisor.Core.Interfaces.Repositories;
using TravelAdvisor.Infrastructure.Data;

namespace TravelAdvisor.Infrastructure.Repositories;

public sealed class HotelRepository(AppDbContext context) : IHotelRepository
{
    public async Task<List<Hotel>> SearchAsync(HotelSearchCriteria criteria, CancellationToken cancellationToken = default)
    {
        var query = WithListIncludes(context.Hotels.AsNoTracking());

        if (criteria.ApprovedOnly)
            query = query.Where(h => h.ApprovalStatus == ApprovalStatus.Approved);
        else if (criteria.ApprovalStatus.HasValue)
            query = query.Where(h => h.ApprovalStatus == criteria.ApprovalStatus.Value);

        if (!string.IsNullOrWhiteSpace(criteria.City))
        {
            var city = criteria.City.Trim().ToLower();
            query = query.Where(h => h.City.ToLower() == city);
        }

        if (!string.IsNullOrWhiteSpace(criteria.Country))
        {
            var country = criteria.Country.Trim().ToLower();
            query = query.Where(h => h.Country.ToLower() == country);
        }

        if (!string.IsNullOrWhiteSpace(criteria.Query))
        {
            var pattern = $"%{EscapeLike(criteria.Query.Trim())}%";
            query = query.Where(h =>
                EF.Functions.ILike(h.Name, pattern, "\\") ||
                EF.Functions.ILike(h.City, pattern, "\\") ||
                EF.Functions.ILike(h.Country, pattern, "\\") ||
                (h.Description != null && EF.Functions.ILike(h.Description, pattern, "\\")));
        }

        if (criteria.MaxPricePerNight.HasValue || criteria.Guests.HasValue)
        {
            var maxPrice = criteria.MaxPricePerNight;
            var guests = criteria.Guests;
            query = query.Where(h => h.Rooms.Any(r =>
                r.IsAvailable &&
                (maxPrice == null || r.PricePerNight <= maxPrice) &&
                (guests == null || r.Capacity >= guests)));
        }

        return await query.OrderBy(h => h.Name).AsSplitQuery().ToListAsync(cancellationToken);
    }

    public Task<List<Hotel>> ListByOwnerAsync(string ownerId, CancellationToken cancellationToken = default) =>
        WithListIncludes(context.Hotels.AsNoTracking())
            .Where(h => h.OwnerId == ownerId)
            .OrderBy(h => h.Name)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

    public Task<Hotel?> GetByIdAsync(int id, bool includeRooms = false, CancellationToken cancellationToken = default)
    {
        IQueryable<Hotel> query = context.Hotels;
        if (includeRooms)
            query = WithListIncludes(query).AsSplitQuery();
        return query.FirstOrDefaultAsync(h => h.Id == id, cancellationToken);
    }

    public Task<bool> HasBookingsAsync(int hotelId, CancellationToken cancellationToken = default) =>
        context.Bookings.AnyAsync(b => b.Room != null && b.Room.HotelId == hotelId, cancellationToken);

    public void Add(Hotel hotel) => context.Hotels.Add(hotel);

    public void Remove(Hotel hotel) => context.Hotels.Remove(hotel);

    private static IQueryable<Hotel> WithListIncludes(IQueryable<Hotel> query) =>
        query.Include(h => h.Rooms)
            .Include(h => h.Reviews.Where(r => r.Status == ReviewStatus.Visible));

    internal static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}

public sealed class RoomRepository(AppDbContext context) : IRoomRepository
{
    public Task<Room?> GetAsync(int hotelId, int roomId, CancellationToken cancellationToken = default) =>
        context.Rooms.Include(r => r.Hotel)
            .FirstOrDefaultAsync(r => r.Id == roomId && r.HotelId == hotelId, cancellationToken);

    public Task<Room?> GetByIdAsync(int roomId, CancellationToken cancellationToken = default) =>
        context.Rooms.Include(r => r.Hotel).FirstOrDefaultAsync(r => r.Id == roomId, cancellationToken);

    public Task<List<Room>> ListByHotelAsync(int hotelId, CancellationToken cancellationToken = default) =>
        context.Rooms.AsNoTracking().Where(r => r.HotelId == hotelId).OrderBy(r => r.Name).ToListAsync(cancellationToken);

    public Task<bool> HasBookingsAsync(int roomId, CancellationToken cancellationToken = default) =>
        context.Bookings.AnyAsync(b => b.RoomId == roomId, cancellationToken);

    public void Add(Room room) => context.Rooms.Add(room);

    public void Remove(Room room) => context.Rooms.Remove(room);
}

public sealed class PackageRepository(AppDbContext context) : IPackageRepository
{
    public async Task<List<TravelPackage>> SearchAsync(PackageSearchCriteria criteria, CancellationToken cancellationToken = default)
    {
        var query = WithIncludes(context.TravelPackages.AsNoTracking());

        if (criteria.ApprovedOnly)
            query = query.Where(p => p.ApprovalStatus == ApprovalStatus.Approved);
        else if (criteria.ApprovalStatus.HasValue)
            query = query.Where(p => p.ApprovalStatus == criteria.ApprovalStatus.Value);

        if (criteria.DestinationId.HasValue)
            query = query.Where(p => p.DestinationId == criteria.DestinationId.Value);

        if (criteria.MaxDurationDays.HasValue)
            query = query.Where(p => p.DurationDays <= criteria.MaxDurationDays.Value);

        if (criteria.MaxPrice.HasValue)
        {
            var max = criteria.MaxPrice.Value;
            query = query.Where(p => p.Price + p.Activities.Sum(a => a.Price) <= max);
        }

        if (!string.IsNullOrWhiteSpace(criteria.Query))
        {
            var pattern = $"%{HotelRepository.EscapeLike(criteria.Query.Trim())}%";
            query = query.Where(p =>
                EF.Functions.ILike(p.Title, pattern, "\\") ||
                (p.Description != null && EF.Functions.ILike(p.Description, pattern, "\\")) ||
                EF.Functions.ILike(p.Destination.Name, pattern, "\\") ||
                EF.Functions.ILike(p.Destination.Country, pattern, "\\"));
        }

        return await query.OrderBy(p => p.Title).AsSplitQuery().ToListAsync(cancellationToken);
    }

    public Task<List<TravelPackage>> ListByAgentAsync(string agentId, CancellationToken cancellationToken = default) =>
        WithIncludes(context.TravelPackages.AsNoTracking())
            .Where(p => p.AgentId == agentId)
            .OrderBy(p => p.Title)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

    public Task<TravelPackage?> GetByIdAsync(int id, bool includeDetails = true, CancellationToken cancellationToken = default)
    {
        IQueryable<TravelPackage> query = context.TravelPackages;
        query = includeDetails ? WithIncludes(query).AsSplitQuery() : query.Include(p => p.Destination);
        return query.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public Task<PackageActivity?> GetActivityAsync(int packageId, int activityId, CancellationToken cancellationToken = default) =>
        context.PackageActivities.Include(a => a.TravelPackage)
            .FirstOrDefaultAsync(a => a.Id == activityId && a.TravelPackageId == packageId, cancellationToken);

    public Task<bool> HasBookingsAsync(int packageId, CancellationToken cancellationToken = default) =>
        context.Bookings.AnyAsync(b => b.TravelPackageId == packageId, cancellationToken);

    public void Add(TravelPackage package) => context.TravelPackages.Add(package);

    public void Remove(TravelPackage package) => context.TravelPackages.Remove(package);

    public void AddActivity(PackageActivity activity) => context.PackageActivities.Add(activity);

    public void RemoveActivity(PackageActivity activity) => context.PackageActivities.Remove(activity);

    private static IQueryable<TravelPackage> WithIncludes(IQueryable<TravelPackage> query) =>
        query.Include(p => p.Destination)
            .Include(p => p.Activities)
            .Include(p => p.Reviews.Where(r => r.Status == ReviewStatus.Visible));
}

public sealed class DestinationRepository(AppDbContext context) : IDestinationRepository
{
    public Task<List<Destination>> ListAsync(string? query = null, CancellationToken cancellationToken = default)
    {
        var destinations = context.Destinations.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query))
        {
            var pattern = $"%{HotelRepository.EscapeLike(query.Trim())}%";
            destinations = destinations.Where(d =>
                EF.Functions.ILike(d.Name, pattern, "\\") ||
                EF.Functions.ILike(d.Country, pattern, "\\") ||
                (d.Description != null && EF.Functions.ILike(d.Description, pattern, "\\")));
        }

        return destinations.OrderBy(d => d.Name).ToListAsync(cancellationToken);
    }

    public Task<Destination?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        context.Destinations.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) =>
        context.Destinations.AnyAsync(d => d.Id == id, cancellationToken);

    public Task<bool> NameExistsAsync(string name, string country, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim().ToLower();
        var normalizedCountry = country.Trim().ToLower();
        return context.Destinations.AnyAsync(d =>
            d.Name.ToLower() == normalizedName &&
            d.Country.ToLower() == normalizedCountry &&
            (excludeId == null || d.Id != excludeId), cancellationToken);
    }

    public Task<int> CountPackagesAsync(int destinationId, CancellationToken cancellationToken = default) =>
        context.TravelPackages.CountAsync(p => p.DestinationId == destinationId, cancellationToken);

    public void Add(Destination destination) => context.Destinations.Add(destination);

    public void Remove(Destination destination) => context.Destinations.Remove(destination);
}
