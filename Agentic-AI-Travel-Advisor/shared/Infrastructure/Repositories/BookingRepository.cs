using Microsoft.EntityFrameworkCore;
using TravelAdvisor.Core.Entities;
using TravelAdvisor.Core.Enums;
using TravelAdvisor.Core.Interfaces.Repositories;
using TravelAdvisor.Infrastructure.Data;

namespace TravelAdvisor.Infrastructure.Repositories;

public sealed class BookingRepository(AppDbContext context) : IBookingRepository
{
    public Task<Booking?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        WithIncludes(context.Bookings).FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public Task<List<Booking>> ListAsync(BookingFilter filter, CancellationToken cancellationToken = default) =>
        Apply(WithIncludes(context.Bookings.AsNoTracking()), filter)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<bool> HasRoomOverlapAsync(int roomId, DateTime checkIn, DateTime checkOut, int? excludeBookingId = null,
        CancellationToken cancellationToken = default) =>
        context.Bookings.AnyAsync(b =>
            b.RoomId == roomId &&
            b.Status != BookingStatus.Cancelled &&
            b.CheckIn < checkOut &&
            b.CheckOut > checkIn &&
            (excludeBookingId == null || b.Id != excludeBookingId), cancellationToken);

    public async Task<List<DateOnly>> GetBookedNightsAsync(int roomId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var start = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var end = to.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var ranges = await context.Bookings.AsNoTracking()
            .Where(b => b.RoomId == roomId && b.Status != BookingStatus.Cancelled && b.CheckIn < end && b.CheckOut > start)
            .Select(b => new { b.CheckIn, b.CheckOut })
            .ToListAsync(cancellationToken);

        var nights = new SortedSet<DateOnly>();
        foreach (var range in ranges)
        {
            for (var night = DateOnly.FromDateTime(range.CheckIn); night < DateOnly.FromDateTime(range.CheckOut); night = night.AddDays(1))
            {
                if (night >= from && night < to)
                    nights.Add(night);
            }
        }
        return nights.ToList();
    }

    public Task<int> CountPackageGuestsAsync(int packageId, DateTime checkIn, CancellationToken cancellationToken = default) =>
        context.Bookings
            .Where(b => b.TravelPackageId == packageId && b.CheckIn == checkIn && b.Status != BookingStatus.Cancelled)
            .SumAsync(b => b.Guests, cancellationToken);

    public Task<bool> HasActivePackageBookingAsync(string userId, int packageId, DateTime checkIn, CancellationToken cancellationToken = default) =>
        context.Bookings.AnyAsync(b =>
            b.UserId == userId &&
            b.TravelPackageId == packageId &&
            b.CheckIn == checkIn &&
            b.Status != BookingStatus.Cancelled, cancellationToken);

    public Task LockRoomAsync(int roomId, CancellationToken cancellationToken = default) =>
        context.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"Rooms\" WHERE \"Id\" = {roomId} FOR UPDATE", cancellationToken);

    public Task LockPackageAsync(int packageId, CancellationToken cancellationToken = default) =>
        context.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"TravelPackages\" WHERE \"Id\" = {packageId} FOR UPDATE", cancellationToken);

    public void Add(Booking booking) => context.Bookings.Add(booking);

    internal static IQueryable<Booking> Apply(IQueryable<Booking> query, BookingFilter filter)
    {
        if (filter.GuestUserId is not null)
            query = query.Where(b => b.UserId == filter.GuestUserId);
        if (filter.HotelOwnerId is not null)
            query = query.Where(b => b.Room != null && b.Room.Hotel.OwnerId == filter.HotelOwnerId);
        if (filter.AgentId is not null)
            query = query.Where(b => b.TravelPackage != null && b.TravelPackage.AgentId == filter.AgentId);
        if (filter.Status.HasValue)
            query = query.Where(b => b.Status == filter.Status.Value);
        if (filter.CreatedFrom.HasValue)
            query = query.Where(b => b.CreatedAt >= filter.CreatedFrom.Value);
        if (filter.CreatedTo.HasValue)
            query = query.Where(b => b.CreatedAt < filter.CreatedTo.Value);
        return query;
    }

    private static IQueryable<Booking> WithIncludes(IQueryable<Booking> query) =>
        query.Include(b => b.Room).ThenInclude(r => r!.Hotel)
            .Include(b => b.TravelPackage)
            .Include(b => b.User);
}

public sealed class ReportRepository(AppDbContext context) : IReportRepository
{
    public async Task<List<BookingStatusTotals>> GetBookingTotalsAsync(BookingFilter filter, CancellationToken cancellationToken = default)
    {
        var rows = await BookingRepository.Apply(context.Bookings.AsNoTracking(), filter)
            .GroupBy(b => b.Status)
            .Select(g => new { Status = g.Key, Count = g.Count(), Total = g.Sum(b => b.TotalPrice) })
            .ToListAsync(cancellationToken);
        return rows.Select(r => new BookingStatusTotals(r.Status, r.Count, r.Total)).ToList();
    }

    public async Task<CatalogCounts> GetCatalogCountsAsync(string? hotelOwnerId, string? agentId, CancellationToken cancellationToken = default)
    {
        var hotels = context.Hotels.AsNoTracking();
        var packages = context.TravelPackages.AsNoTracking();
        if (hotelOwnerId is not null)
            hotels = hotels.Where(h => h.OwnerId == hotelOwnerId);
        if (agentId is not null)
            packages = packages.Where(p => p.AgentId == agentId);

        var scoped = hotelOwnerId is not null || agentId is not null;
        return new CatalogCounts(
            Users: scoped ? 0 : await context.Users.CountAsync(cancellationToken),
            HotelOwners: scoped ? 0 : await context.Users.CountAsync(u => u.Role.Name == RoleNames.HotelOwner, cancellationToken),
            TravelAgents: scoped ? 0 : await context.Users.CountAsync(u => u.Role.Name == RoleNames.TravelAgent, cancellationToken),
            Hotels: agentId is not null ? 0 : await hotels.CountAsync(cancellationToken),
            Packages: hotelOwnerId is not null ? 0 : await packages.CountAsync(cancellationToken),
            PendingHotels: agentId is not null ? 0 : await hotels.CountAsync(h => h.ApprovalStatus == ApprovalStatus.Pending, cancellationToken),
            PendingPackages: hotelOwnerId is not null ? 0 : await packages.CountAsync(p => p.ApprovalStatus == ApprovalStatus.Pending, cancellationToken));
    }
}
