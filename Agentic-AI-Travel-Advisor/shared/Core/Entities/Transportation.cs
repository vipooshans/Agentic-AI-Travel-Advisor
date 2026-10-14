using TravelAdvisor.Core.Enums;

namespace TravelAdvisor.Core.Entities;

/// <summary>A transport option managed by a travel agent, optionally tied to a package and/or destination.</summary>
public class Transportation : IAuditable
{
    public int Id { get; set; }
    public string ProviderId { get; set; } = string.Empty;
    public int? TravelPackageId { get; set; }
    public int? DestinationId { get; set; }
    public TransportMode Mode { get; set; }
    public string FromLocation { get; set; } = string.Empty;
    public string ToLocation { get; set; } = string.Empty;
    public TimeSpan? DepartureTime { get; set; }
    public int DurationMinutes { get; set; }
    public decimal PricePerPerson { get; set; }
    public int Capacity { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser Provider { get; set; } = null!;
    public TravelPackage? TravelPackage { get; set; }
    public Destination? Destination { get; set; }
}
