namespace TravelAdvisor.Core.DTOs.Bookings;

public class BookingDto
{
    public int Id { get; set; }
    public int? RoomId { get; set; }
    public string? RoomName { get; set; }
    public string? HotelName { get; set; }
    public int? TravelPackageId { get; set; }
    public string? TravelPackageTitle { get; set; }
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
    public DateTime CreatedAt { get; set; }
}
