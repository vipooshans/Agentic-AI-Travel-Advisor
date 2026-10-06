using TravelAdvisor.Core.Enums;

namespace TravelAdvisor.Core.DTOs.Bookings;

public class BookingDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int? RoomId { get; set; }
    public int? TravelPackageId { get; set; }
    public int? HotelId { get; set; }
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public int Guests { get; set; }
    public string? Notes { get; set; }
    public BookingStatus Status { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? RoomName { get; set; }
    public string? HotelName { get; set; }
    public string? PackageTitle { get; set; }
    public string? UserEmail { get; set; }
}

public class CreateBookingRequest
{
    public int? RoomId { get; set; }
    public int? TravelPackageId { get; set; }
    /// <summary>Arrival date. Only the date part is used.</summary>
    public DateTime CheckIn { get; set; }
    /// <summary>Departure date for room bookings. Package bookings end after the package duration.</summary>
    public DateTime? CheckOut { get; set; }
    public int Guests { get; set; } = 1;
    public string? Notes { get; set; }
}

public class UpdateBookingStatusRequest
{
    public BookingStatus Status { get; set; }
}

/// <summary>Asks whether a room or package can be booked for the given dates and party size.</summary>
public class AvailabilityQuery
{
    public int? RoomId { get; set; }
    public int? TravelPackageId { get; set; }
    public DateTime CheckIn { get; set; }
    public DateTime? CheckOut { get; set; }
    public int Guests { get; set; } = 1;
}

public class AvailabilityQuoteDto
{
    public bool Available { get; set; }
    public string? Reason { get; set; }
    public int? RoomId { get; set; }
    public int? TravelPackageId { get; set; }
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public int Nights { get; set; }
    public int Guests { get; set; }
    public decimal TotalPrice { get; set; }
    /// <summary>For packages: places still free on the chosen start date.</summary>
    public int? RemainingPlaces { get; set; }
}

public class RoomCalendarEntryDto
{
    public DateOnly Date { get; set; }
    public bool IsBlocked { get; set; }
    public decimal? PriceOverride { get; set; }
    public string? Note { get; set; }
}

public class RoomCalendarDto
{
    public int RoomId { get; set; }
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public decimal BasePricePerNight { get; set; }
    public List<RoomCalendarEntryDto> Overrides { get; set; } = [];
    /// <summary>Nights already taken by pending or confirmed bookings.</summary>
    public List<DateOnly> BookedNights { get; set; } = [];
}

public class SaveRoomCalendarRequest
{
    public List<RoomCalendarEntryDto> Entries { get; set; } = [];
}
