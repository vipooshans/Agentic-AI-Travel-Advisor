using TravelAdvisor.Core.Enums;

namespace TravelAdvisor.Core.Entities;

/// <summary>One catalog item the AI recommended in a conversation turn, with the reason and cost it used.</summary>
public class AIRecommendation
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public string UserId { get; set; } = string.Empty;
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
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public AIConversation Conversation { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}
