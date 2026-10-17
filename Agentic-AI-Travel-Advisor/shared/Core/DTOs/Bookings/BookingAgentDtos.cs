using TravelAdvisor.Core.Enums;

namespace TravelAdvisor.Core.DTOs.Bookings;

public class BookingAgentPromptRequest
{
    public int? ConversationId { get; set; }
    public string Prompt { get; set; } = string.Empty;
    public List<string> PreviousPrompts { get; set; } = [];
}

public class BookingProposalDto
{
    public string Kind { get; set; } = "hotel"; // "hotel" or "package"
    public int? HotelId { get; set; }
    public string? HotelName { get; set; }
    public int? RoomId { get; set; }
    public string? RoomName { get; set; }
    public int? TravelPackageId { get; set; }
    public string? PackageTitle { get; set; }
    public DateTime CheckIn { get; set; }
    public DateTime? CheckOut { get; set; }
    public int Nights { get; set; }
    public int Guests { get; set; }
    public decimal TotalPrice { get; set; }
    public string? Notes { get; set; }
    public string ProposalToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public class CancellationPreviewDto
{
    public int BookingId { get; set; }
    public string ListingName { get; set; } = string.Empty;
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public decimal TotalPrice { get; set; }
    public BookingStatus Status { get; set; }
    public bool CanCancel { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class BookingInsightDto
{
    public int BookingId { get; set; }
    public string Type { get; set; } = "info"; // "urgent", "advisory", "tip", "success"
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ActionUrl { get; set; }
    public string? ActionLabel { get; set; }
}

public class BookingSuggestionDto
{
    public string Label { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public string Type { get; set; } = "general"; // "budget_down", "budget_up", "place_change", "date_change"
}

public class BookingAgentMessageDto
{
    public string Role { get; set; } = "assistant"; // "user", "assistant"
    public string Content { get; set; } = string.Empty;
    public BookingProposalDto? Proposal { get; set; }
    public CancellationPreviewDto? Cancellation { get; set; }
    public BookingDto? Booking { get; set; }
    public List<BookingSuggestionDto> Suggestions { get; set; } = [];
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class BookingAgentPromptResponse
{
    public int ConversationId { get; set; }
    public string ActionType { get; set; } = "info"; // "chat", "proposal", "cancellation", "insights", "info", "booking_confirmed", "error"
    public string Message { get; set; } = string.Empty;
    public BookingProposalDto? Proposal { get; set; }
    public CancellationPreviewDto? Cancellation { get; set; }
    public BookingDto? Booking { get; set; }
    public List<BookingInsightDto>? Insights { get; set; }
    public List<BookingSuggestionDto> Suggestions { get; set; } = [];
    public List<BookingAgentMessageDto> History { get; set; } = [];
}

public class ConfirmBookingAgentRequest
{
    public string ProposalToken { get; set; } = string.Empty;
    public int? RoomId { get; set; }
    public int? TravelPackageId { get; set; }
    public DateTime CheckIn { get; set; }
    public DateTime? CheckOut { get; set; }
    public int Guests { get; set; } = 1;
    public string? Notes { get; set; }
}
