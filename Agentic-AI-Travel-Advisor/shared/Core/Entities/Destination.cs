namespace TravelAdvisor.Core.Entities;

public class Destination
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }

    public ICollection<TravelPackage> TravelPackages { get; set; } = [];
}
