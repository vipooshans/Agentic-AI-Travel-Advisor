using TravelAdvisor.Core.DTOs.AI;

namespace TravelAdvisor.Core.Interfaces;

public interface IAgenticAiService
{
    Task<ChatResponse> ChatAsync(string userId, ChatRequest request, CancellationToken cancellationToken = default);

    Task<List<ConversationSummaryDto>> ListConversationsAsync(string userId, CancellationToken cancellationToken = default);

    Task<ConversationDetailDto?> GetConversationAsync(string userId, int id, CancellationToken cancellationToken = default);
}
