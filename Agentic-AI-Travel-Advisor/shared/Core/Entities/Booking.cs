using TravelAdvisor.Core.Enums;

namespace TravelAdvisor.Core.Entities;

public class Booking : IAuditable
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int? RoomId { get; set; }
    public int? TravelPackageId { get; set; }
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public int Guests { get; set; } = 1;
    public string? Notes { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Pending;
    public decimal TotalPrice { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CancelledAt { get; set; }

    /// <summary>Maps to PostgreSQL xmin for optimistic concurrency on status changes.</summary>
    public uint Version { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public Room? Room { get; set; }
    public TravelPackage? TravelPackage { get; set; }
    public ICollection<Payment> Payments { get; set; } = [];
    public Review? Review { get; set; }
}
