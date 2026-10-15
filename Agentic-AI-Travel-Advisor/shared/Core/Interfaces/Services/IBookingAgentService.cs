using TravelAdvisor.Core.Common;
using TravelAdvisor.Core.DTOs.Bookings;

namespace TravelAdvisor.Core.Interfaces.Services;

public interface IBookingAgentService
{
    Task<BookingAgentPromptResponse> ProcessPromptAsync(UserContext caller, BookingAgentPromptRequest request, CancellationToken cancellationToken = default);
    Task<BookingAgentPromptResponse> GetActiveConversationAsync(UserContext caller, CancellationToken cancellationToken = default);
    Task<BookingDto> ConfirmProposalAsync(UserContext caller, ConfirmBookingAgentRequest request, CancellationToken cancellationToken = default);
    Task<List<BookingInsightDto>> GetBookingInsightsAsync(UserContext caller, CancellationToken cancellationToken = default);
}
