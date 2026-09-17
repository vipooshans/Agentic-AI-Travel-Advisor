namespace TravelAdvisor.Core.DTOs.Destinations;

public class DestinationDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
}

public class DestinationDetailDto : DestinationDto
{
    public int PackageCount { get; set; }
}
