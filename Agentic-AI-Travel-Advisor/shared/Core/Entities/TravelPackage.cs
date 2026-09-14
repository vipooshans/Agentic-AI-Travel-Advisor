namespace TravelAdvisor.Core.Entities;

public class TravelPackage
{
    public int Id { get; set; }
    public string AgentId { get; set; } = string.Empty;
    public int DestinationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int DurationDays { get; set; }

    public ApplicationUser Agent { get; set; } = null!;
    public Destination Destination { get; set; } = null!;
    public ICollection<Booking> Bookings { get; set; } = [];
}
