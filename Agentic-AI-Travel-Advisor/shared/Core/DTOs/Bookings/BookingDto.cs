using TravelAdvisor.Core.Enums;

namespace TravelAdvisor.Core.DTOs.Bookings;

public class BookingDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int? RoomId { get; set; }
    public int? TravelPackageId { get; set; }
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public BookingStatus Status { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? RoomName { get; set; }
    public string? HotelName { get; set; }
    public string? PackageTitle { get; set; }
    public string? UserEmail { get; set; }
}

public class CreateBookingRequest
{
    public int? RoomId { get; set; }
    public int? TravelPackageId { get; set; }
    public DateTime CheckIn { get; set; }
    public DateTime? CheckOut { get; set; }
}

public class UpdateBookingStatusRequest
{
    public BookingStatus Status { get; set; }
}
