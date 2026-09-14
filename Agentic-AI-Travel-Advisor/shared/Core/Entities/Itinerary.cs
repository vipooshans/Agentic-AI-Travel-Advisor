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

    public ApplicationUser User { get; set; } = null!;
    public ICollection<ItineraryItem> Items { get; set; } = [];
}
