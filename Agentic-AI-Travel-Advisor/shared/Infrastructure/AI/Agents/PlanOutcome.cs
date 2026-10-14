using TravelAdvisor.Core.DTOs.AI;
using TravelAdvisor.Core.Enums;

namespace TravelAdvisor.Infrastructure.AI.Agents;

public static class AgentNames
{
    public const string Orchestrator = "Orchestrator";
    public const string LlmOrchestrator = "LlmOrchestrator";
    public const string SafetyGuard = "SafetyGuard";
    public const string Planning = "TravelPlanningAgent";
    public const string Recommendation = "RecommendationAgent";
    public const string Itinerary = "ItineraryAgent";
    public const string Booking = "BookingAgent";
}

/// <summary>Result of generateItinerary: a validated plan, or the reason no plan could be made.</summary>
public sealed class PlanOutcome
{
    /// <summary>One of <see cref="ChatStatus"/>: plan, over_budget, no_match or info (plan failed validation).</summary>
    public string Status { get; init; } = ChatStatus.Plan;
    public string Message { get; set; } = string.Empty;
    public TripRequirements? Requirements { get; init; }
    public TravelPlan? Plan { get; init; }
    public SuggestedTravelPlan? LegacyPlan { get; init; }
    public List<RecommendationRecord> Recommendations { get; init; } = [];
}

public sealed record RecommendationRecord(
    RecommendationType Type,
    int? HotelId,
    int? RoomId,
    int? TravelPackageId,
    int? TransportationId,
    int? DestinationId,
    string Title,
    decimal EstimatedCost,
    double Score,
    string Reason);
