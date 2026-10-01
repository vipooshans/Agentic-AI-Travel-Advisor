using TravelAdvisor.Core.Enums;

namespace TravelAdvisor.Core.Entities;

public class TravelPackage : IAuditable
{
    public int Id { get; set; }
    public string AgentId { get; set; } = string.Empty;
    public int DestinationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>Base price per person, excluding activity prices.</summary>
    public decimal Price { get; set; }
    public int DurationDays { get; set; }
    public string? ImageUrl { get; set; }

    /// <summary>Maximum travelers across all bookings that start on the same date.</summary>
    public int MaxTravelers { get; set; } = 10;
    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser Agent { get; set; } = null!;
    public Destination Destination { get; set; } = null!;
    public ICollection<PackageActivity> Activities { get; set; } = [];
    public ICollection<Booking> Bookings { get; set; } = [];
    public ICollection<Transportation> Transportation { get; set; } = [];
    public ICollection<Review> Reviews { get; set; } = [];
}
