using TravelAdvisor.Core.Enums;

namespace TravelAdvisor.Core.DTOs.Packages;

public class TravelPackageDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int DurationDays { get; set; }
    public int DestinationId { get; set; }
    public string DestinationName { get; set; } = string.Empty;
    public string DestinationCountry { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int ActivityCount { get; set; }
    public ApprovalStatus ApprovalStatus { get; set; }

    /// <summary>Package price plus all included activity prices, per person.</summary>
    public decimal TotalPrice { get; set; }
    public int MaxTravelers { get; set; }
    public double? AverageRating { get; set; }
    public int ReviewCount { get; set; }
}

public class TravelPackageDetailDto : TravelPackageDto
{
    public List<PackageActivityDto> Activities { get; set; } = [];
}

public class PackageSearchQuery
{
    public string? Q { get; set; }
    public int? DestinationId { get; set; }
    public ApprovalStatus? ApprovalStatus { get; set; }
    public decimal? MaxPrice { get; set; }
    public int? MaxDurationDays { get; set; }
}

public class CreatePackageRequest
{
    public int DestinationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int DurationDays { get; set; }
    public string? ImageUrl { get; set; }
    public int MaxTravelers { get; set; } = 10;
}

public class UpdatePackageRequest
{
    public int DestinationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int DurationDays { get; set; }
    public string? ImageUrl { get; set; }
    public int MaxTravelers { get; set; } = 10;
}

public class PackageActivityDto
{
    public int Id { get; set; }
    public int TravelPackageId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DayNumber { get; set; }
    public decimal Price { get; set; }
    public int SortOrder { get; set; }
}

public class CreateActivityRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DayNumber { get; set; }
    public decimal Price { get; set; }
    public int SortOrder { get; set; }
}
