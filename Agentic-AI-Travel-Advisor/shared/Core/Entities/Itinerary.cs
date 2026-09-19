using TravelAdvisor.Core.Enums;

namespace TravelAdvisor.Core.Entities;

public class Itinerary
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public ItineraryStatus Status { get; set; } = ItineraryStatus.Draft;
    public decimal? EstimatedCost { get; set; }
    public int? DestinationId { get; set; }
    public string? Summary { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public Destination? Destination { get; set; }
    public ICollection<ItineraryItem> Items { get; set; } = [];
}
