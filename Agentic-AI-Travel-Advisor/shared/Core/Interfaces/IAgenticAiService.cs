using TravelAdvisor.Core.Common;
using TravelAdvisor.Core.DTOs.AI;

namespace TravelAdvisor.Core.Interfaces;

public interface IAgenticAiService
{
    /// <summary>The caller's role is needed because every AI tool is authorized per role.</summary>
    Task<ChatResponse> ChatAsync(UserContext caller, ChatRequest request, CancellationToken cancellationToken = default);

    Task<List<ConversationSummaryDto>> ListConversationsAsync(string userId, CancellationToken cancellationToken = default);

    Task<ConversationDetailDto?> GetConversationAsync(string userId, int id, CancellationToken cancellationToken = default);

    Task<List<AiRecommendationDto>> ListRecommendationsAsync(string userId, int? conversationId, CancellationToken cancellationToken = default);
}
