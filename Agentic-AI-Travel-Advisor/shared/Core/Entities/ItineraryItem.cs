namespace TravelAdvisor.Core.Entities;

public class ItineraryItem
{
    public int Id { get; set; }
    public int ItineraryId { get; set; }
    public int DayNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TimeSpan? StartTime { get; set; }
    public int SortOrder { get; set; }

    /// <summary>hotel, activity, transport, meal or free.</summary>
    public string? ItemType { get; set; }
    public decimal? EstimatedCost { get; set; }

    public Itinerary Itinerary { get; set; } = null!;
}
