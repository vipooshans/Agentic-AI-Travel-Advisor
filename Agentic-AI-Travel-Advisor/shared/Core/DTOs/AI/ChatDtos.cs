namespace TravelAdvisor.Core.DTOs.AI;

public class ChatRequest
{
    public int? ConversationId { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class ChatResponse
{
    public int ConversationId { get; set; }
    public string Message { get; set; } = string.Empty;
    public SuggestedTravelPlan? SuggestedPlan { get; set; }
}

public class ChatMessageDto
{
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;
    public SuggestedTravelPlan? SuggestedPlan { get; set; }
}

public class ConversationSummaryDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}

public class ConversationDetailDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<ChatMessageDto> Messages { get; set; } = [];
}

public class TripRequirements
{
    public string? Destination { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? DurationDays { get; set; }
    public int Travelers { get; set; } = 2;
    public decimal? Budget { get; set; }
    public string? Interests { get; set; }
    public string? AccommodationPreference { get; set; }
    public List<string> MissingFields { get; set; } = [];

    public bool HasEnoughToPlan =>
        !string.IsNullOrWhiteSpace(Destination) && Budget is > 0;
}

public class SuggestedTravelPlan
{
    public string Title { get; set; } = string.Empty;
    public int? DestinationId { get; set; }
    public string DestinationName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int Travelers { get; set; }
    public decimal Budget { get; set; }
    public decimal EstimatedCost { get; set; }
    public string? Summary { get; set; }
    public SuggestedHotel? Hotel { get; set; }
    public SuggestedPackage? Package { get; set; }
    public List<SuggestedPlanItem> Items { get; set; } = [];
}

public class SuggestedHotel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public int? RoomId { get; set; }
    public string? RoomName { get; set; }
    public decimal PricePerNight { get; set; }
}

public class SuggestedPackage
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int DurationDays { get; set; }
}

public class SuggestedPlanItem
{
    public int DayNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TimeSpan? StartTime { get; set; }
    public int SortOrder { get; set; }
}

public class CatalogHotelMatch
{
    public int HotelId { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public decimal PricePerNight { get; set; }
    public int Capacity { get; set; }
}

public class CatalogPackageMatch
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int DurationDays { get; set; }
    public int DestinationId { get; set; }
    public string DestinationName { get; set; } = string.Empty;
    public List<CatalogActivityMatch> Activities { get; set; } = [];
}

public class CatalogActivityMatch
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DayNumber { get; set; }
    public decimal Price { get; set; }
    public int SortOrder { get; set; }
}

public class CatalogDestinationMatch
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string? Description { get; set; }
}
