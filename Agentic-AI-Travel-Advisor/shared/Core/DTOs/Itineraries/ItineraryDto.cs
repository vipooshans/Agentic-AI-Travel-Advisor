namespace TravelAdvisor.Core.DTOs.Itineraries;

public class ItineraryDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int Status { get; set; }
    public decimal? EstimatedCost { get; set; }
    public decimal? Budget { get; set; }
    public int Travelers { get; set; }
    public int? DestinationId { get; set; }
    public string? DestinationName { get; set; }
    public int? ConversationId { get; set; }
    public string? Summary { get; set; }
    public int ItemCount { get; set; }
}

public class ItineraryDetailDto : ItineraryDto
{
    public List<ItineraryItemDto> Items { get; set; } = [];
}

public class ItineraryItemDto
{
    public int Id { get; set; }
    public int DayNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TimeSpan? StartTime { get; set; }
    public int SortOrder { get; set; }
}

public class CreateItineraryRequest
{
    public string Title { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int? DestinationId { get; set; }
    public decimal? EstimatedCost { get; set; }
    public decimal? Budget { get; set; }
    /// <summary>Defaults to 1 when omitted.</summary>
    public int? Travelers { get; set; }
    /// <summary>The AI conversation the plan came from; must belong to the caller.</summary>
    public int? ConversationId { get; set; }
    public string? Summary { get; set; }
    public List<CreateItineraryItemRequest> Items { get; set; } = [];
}

public class CreateItineraryItemRequest
{
    public int DayNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TimeSpan? StartTime { get; set; }
    public int SortOrder { get; set; }
}
