namespace TravelAdvisor.Core.Entities;

public class Destination : IAuditable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TravelPackage> TravelPackages { get; set; } = [];
    public ICollection<Itinerary> Itineraries { get; set; } = [];
    public ICollection<Transportation> Transportation { get; set; } = [];
}
