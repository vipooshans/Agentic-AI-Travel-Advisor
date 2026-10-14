using TravelAdvisor.Core.DTOs.Bookings;
using TravelAdvisor.Core.Enums;

namespace TravelAdvisor.Core.DTOs.AI;

public class ChatRequest
{
    public int? ConversationId { get; set; }
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Optional id of the booking proposal the user is confirming (e.g. from a "Confirm" button).
    /// A plain "confirm" message works too; either way only the latest pending proposal can be confirmed.
    /// </summary>
    public string? ConfirmBookingId { get; set; }
}

public static class ChatStatus
{
    public const string Plan = "plan";
    public const string Clarification = "clarification";
    public const string NoMatch = "no_match";
    public const string OverBudget = "over_budget";
    public const string BookingProposal = "booking_proposal";
    public const string BookingCreated = "booking_created";
    public const string BookingFailed = "booking_failed";
    public const string Refused = "refused";
    public const string Info = "info";
}

public class ChatResponse
{
    public int ConversationId { get; set; }
    public string Message { get; set; } = string.Empty;

    /// <summary>Legacy single-hotel/single-package plan kept for existing clients (Flutter, MVC).</summary>
    public SuggestedTravelPlan? SuggestedPlan { get; set; }

    /// <summary>One of <see cref="ChatStatus"/>.</summary>
    public string Status { get; set; } = ChatStatus.Info;

    /// <summary>Schema-validated structured plan built from backend tool results.</summary>
    public TravelPlan? Plan { get; set; }

    /// <summary>A booking waiting for the user's explicit confirmation. Nothing is booked yet.</summary>
    public BookingProposal? PendingBooking { get; set; }

    /// <summary>The booking exactly as the backend created it (status is whatever the backend returned).</summary>
    public BookingDto? Booking { get; set; }

    /// <summary>"llm" when the model drove tool selection, otherwise "deterministic".</summary>
    public string Mode { get; set; } = "deterministic";

    public List<string> Agents { get; set; } = [];
    public List<ToolCallSummary> ToolCalls { get; set; } = [];
}

public class ChatMessageDto
{
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;
    public SuggestedTravelPlan? SuggestedPlan { get; set; }
    public string? Status { get; set; }
    public TravelPlan? Plan { get; set; }
    public BookingProposal? PendingBooking { get; set; }
    public int? BookingId { get; set; }
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

public class AiRecommendationDto
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public RecommendationType ItemType { get; set; }
    public int? HotelId { get; set; }
    public int? RoomId { get; set; }
    public int? TravelPackageId { get; set; }
    public int? TransportationId { get; set; }
    public int? DestinationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal EstimatedCost { get; set; }
    public double Score { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TripRequirements
{
    public string? Destination { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? DurationDays { get; set; }
    public int Travelers { get; set; } = 2;

    /// <summary>Total trip budget in LKR (converted when the user gave another currency).</summary>
    public decimal? Budget { get; set; }
    public string Currency { get; set; } = "LKR";
    public decimal? OriginalBudget { get; set; }
    public string? OriginalCurrency { get; set; }
    public string? Interests { get; set; }
    public string? AccommodationPreference { get; set; }

    /// <summary>A <see cref="TransportMode"/> name such as Train.</summary>
    public string? TransportPreference { get; set; }
    public string? Origin { get; set; }
    public List<string> MissingFields { get; set; } = [];
    public List<string> Assumptions { get; set; } = [];
    public List<string> Warnings { get; set; } = [];

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
    public double? AverageRating { get; set; }
}

public class CatalogPackageMatch
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>Base price per traveler, excluding activity prices.</summary>
    public decimal Price { get; set; }
    public int DurationDays { get; set; }
    public int DestinationId { get; set; }
    public string DestinationName { get; set; } = string.Empty;
    public int MaxTravelers { get; set; }
    public double? AverageRating { get; set; }
    public List<CatalogActivityMatch> Activities { get; set; } = [];

    /// <summary>What one traveler pays: base price plus every activity, matching BookingService pricing.</summary>
    public decimal PricePerPerson => Price + Activities.Sum(a => a.Price);
}

public class CatalogActivityMatch
{
    public int Id { get; set; }
    public int TravelPackageId { get; set; }
    public string? PackageTitle { get; set; }
    public string? DestinationName { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public int DayNumber { get; set; }
    public decimal Price { get; set; }
    public int SortOrder { get; set; }
}

public class CatalogTransportMatch
{
    public int Id { get; set; }
    public TransportMode Mode { get; set; }
    public string FromLocation { get; set; } = string.Empty;
    public string ToLocation { get; set; } = string.Empty;
    public int? DestinationId { get; set; }
    public int? TravelPackageId { get; set; }
    public TimeSpan? DepartureTime { get; set; }
    public int DurationMinutes { get; set; }
    public decimal PricePerPerson { get; set; }
    public int Capacity { get; set; }
    public string? Description { get; set; }
}

public class CatalogDestinationMatch
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string? Description { get; set; }
}
