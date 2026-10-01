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

    void Add(Booking booking);
}
