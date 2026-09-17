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
    public int ActivityCount { get; set; }
}

public class TravelPackageDetailDto : TravelPackageDto
{
    public List<PackageActivityDto> Activities { get; set; } = [];
}

public class CreatePackageRequest
{
    public int DestinationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int DurationDays { get; set; }
}

public class UpdatePackageRequest
{
    public int DestinationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int DurationDays { get; set; }
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
