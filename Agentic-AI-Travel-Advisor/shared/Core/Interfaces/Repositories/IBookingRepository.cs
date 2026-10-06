using TravelAdvisor.Core.Entities;
using TravelAdvisor.Core.Enums;

namespace TravelAdvisor.Core.Interfaces.Repositories;

/// <summary>Restricts a booking query to what a caller may see. Null fields are not filtered.</summary>
public sealed class BookingFilter
{
    public string? GuestUserId { get; init; }
    public string? HotelOwnerId { get; init; }
    public string? AgentId { get; init; }
    public BookingStatus? Status { get; init; }
    public DateTime? CreatedFrom { get; init; }
    public DateTime? CreatedTo { get; init; }
}

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<Booking>> ListAsync(BookingFilter filter, CancellationToken cancellationToken = default);

    /// <summary>True when a non-cancelled booking on the room overlaps [checkIn, checkOut).</summary>
    Task<bool> HasRoomOverlapAsync(int roomId, DateTime checkIn, DateTime checkOut, int? excludeBookingId = null,
        CancellationToken cancellationToken = default);

    /// <summary>Nights in [from, to) already taken by non-cancelled bookings of the room.</summary>
    Task<List<DateOnly>> GetBookedNightsAsync(int roomId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    /// <summary>Total guests on non-cancelled bookings of the package that start on <paramref name="checkIn"/>.</summary>
    Task<int> CountPackageGuestsAsync(int packageId, DateTime checkIn, CancellationToken cancellationToken = default);

    Task<bool> HasActivePackageBookingAsync(string userId, int packageId, DateTime checkIn, CancellationToken cancellationToken = default);

    /// <summary>Takes a row lock on the room (inside the current transaction) to serialise concurrent bookings.</summary>
    Task LockRoomAsync(int roomId, CancellationToken cancellationToken = default);

    /// <summary>Takes a row lock on the package (inside the current transaction) to serialise concurrent bookings.</summary>
    Task LockPackageAsync(int packageId, CancellationToken cancellationToken = default);

    /// <summary>Takes a row lock on the booking (inside the current transaction), e.g. to serialise payments.</summary>
    Task LockBookingAsync(int bookingId, CancellationToken cancellationToken = default);

    void Add(Booking booking);
}

public interface IRoomAvailabilityRepository
{
    Task<List<RoomAvailability>> ListAsync(int roomId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
    void Add(RoomAvailability entry);
    void Remove(RoomAvailability entry);
}

public interface ISystemSettingRepository
{
    Task<SystemSetting?> GetAsync(string key, CancellationToken cancellationToken = default);
    Task<List<SystemSetting>> ListAsync(CancellationToken cancellationToken = default);
    void Add(SystemSetting setting);
}
