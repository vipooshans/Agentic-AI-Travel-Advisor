using TravelAdvisor.Core.Enums;

namespace TravelAdvisor.Core.Entities;

/// <summary>A traveler's review of a hotel or package, tied to one completed booking.</summary>
public class Review : IAuditable
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int BookingId { get; set; }
    public int? HotelId { get; set; }
    public int? TravelPackageId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public ReviewStatus Status { get; set; } = ReviewStatus.Visible;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser User { get; set; } = null!;
    public Booking Booking { get; set; } = null!;
    public Hotel? Hotel { get; set; }
    public TravelPackage? TravelPackage { get; set; }
}
