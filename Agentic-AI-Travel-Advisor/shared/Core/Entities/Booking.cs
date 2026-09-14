using TravelAdvisor.Core.Enums;

namespace TravelAdvisor.Core.Entities;

public class Booking
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int? RoomId { get; set; }
    public int? TravelPackageId { get; set; }
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Pending;
    public decimal TotalPrice { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser User { get; set; } = null!;
    public Room? Room { get; set; }
    public TravelPackage? TravelPackage { get; set; }
}
