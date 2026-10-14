using TravelAdvisor.Core.Enums;

namespace TravelAdvisor.Core.DTOs.Transportation;

public class TransportationDto
{
    public int Id { get; set; }
    public string ProviderId { get; set; } = string.Empty;
    public int? TravelPackageId { get; set; }
    public string? PackageTitle { get; set; }
    public int? DestinationId { get; set; }
    public string? DestinationName { get; set; }
    public TransportMode Mode { get; set; }
    public string FromLocation { get; set; } = string.Empty;
    public string ToLocation { get; set; } = string.Empty;
    /// <summary>Local departure time as HH:mm, if scheduled.</summary>
    public string? DepartureTime { get; set; }
    public int DurationMinutes { get; set; }
    public decimal PricePerPerson { get; set; }
    public int Capacity { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class SaveTransportationRequest
{
    public int? TravelPackageId { get; set; }
    public int? DestinationId { get; set; }
    public TransportMode Mode { get; set; }
    public string FromLocation { get; set; } = string.Empty;
    public string ToLocation { get; set; } = string.Empty;
    /// <summary>Optional HH:mm.</summary>
    public string? DepartureTime { get; set; }
    public int DurationMinutes { get; set; }
    public decimal PricePerPerson { get; set; }
    public int Capacity { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public class TransportationSearchQuery
{
    public string? From { get; set; }
    public string? To { get; set; }
    public int? DestinationId { get; set; }
    public int? TravelPackageId { get; set; }
    public TransportMode? Mode { get; set; }
    public decimal? MaxPrice { get; set; }
}
