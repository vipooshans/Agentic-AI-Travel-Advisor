using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TravelAdvisor.Core.DTOs.AI;
using TravelAdvisor.Core.Entities;
using TravelAdvisor.Core.Interfaces;
using TravelAdvisor.Infrastructure.AI.Agents;
using TravelAdvisor.Infrastructure.Data;

namespace TravelAdvisor.Infrastructure.AI;

public class AgenticAiService : IAgenticAiService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly AppDbContext _context;
    private readonly ICatalogTools _catalog;
    private readonly ILlmClient _llm;
    private readonly TravelPlanningAgent _planning;
    private readonly RecommendationAgent _recommendation;
    private readonly ItineraryAgent _itinerary;

    public AgenticAiService(
        AppDbContext context,
        ICatalogTools catalog,
        ILlmClient llm,
        TravelPlanningAgent planning,
        RecommendationAgent recommendation,
        ItineraryAgent itinerary)
    {
        _context = context;
        _catalog = catalog;
        _llm = llm;
        _planning = planning;
        _recommendation = recommendation;
        _itinerary = itinerary;
    }

    public async Task<ChatResponse> ChatAsync(string userId, ChatRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            throw new ArgumentException("Message is required.");

        var conversation = await GetOrCreateConversationAsync(userId, request, cancellationToken);
        var messages = Deserialize(conversation.Messages);

        messages.Add(new ChatMessageDto { Role = "user", Content = request.Message.Trim() });

        var knownDestinations = await _catalog.GetDestinationNamesAsync(cancellationToken);
        var requirements = await _planning.ExtractAsync(messages, knownDestinations, cancellationToken);

        string reply;
        SuggestedTravelPlan? plan = null;

        if (!requirements.HasEnoughToPlan)
        {
            reply = _planning.BuildClarifyingMessage(requirements);
            if (_llm.IsConfigured)
            {
                try
                {
                    var polished = await _llm.CompleteAsync(
                        "You are a friendly travel advisor. Rewrite the following as a short clarifying question. Keep it under 40 words.",
                        reply,
                        jsonMode: false,
                        cancellationToken);
                    if (!string.IsNullOrWhiteSpace(polished))
                        reply = polished.Trim('"');
                }
                catch
                {
                    // keep template reply
                }
            }
        }
        else
        {
            var rec = await _recommendation.RecommendAsync(requirements, cancellationToken);
            if (rec.Error is not null)
            {
                reply = rec.Error;
            }
            else
            {
                plan = _itinerary.Build(requirements, rec);
                reply = _itinerary.BuildReply(requirements, plan);
                if (_llm.IsConfigured)
                {
                    try
                    {
                        var polished = await _llm.CompleteAsync(
                            "You are a friendly Sri Lankan travel advisor. Rewrite the plan as a helpful chat reply. Keep the hotel, package, cost, assumptions, and day-by-day items. Do not invent places that are not listed.",
                            reply,
                            jsonMode: false,
                            cancellationToken);
                        if (!string.IsNullOrWhiteSpace(polished))
                            reply = polished;
                    }
                    catch
                    {
                        // keep structured reply
                    }
                }
            }
        }

        messages.Add(new ChatMessageDto
        {
            Role = "assistant",
            Content = reply,
            SuggestedPlan = plan
        });

        if (string.IsNullOrWhiteSpace(conversation.Title) || conversation.Title == "New conversation")
            conversation.Title = TruncateTitle(request.Message);

        conversation.Messages = JsonSerializer.Serialize(messages, JsonOptions);
        conversation.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return new ChatResponse
        {
            ConversationId = conversation.Id,
            Message = reply,
            SuggestedPlan = plan
        };
    }

    public async Task<List<ConversationSummaryDto>> ListConversationsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.AIConversations
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.UpdatedAt)
            .Select(c => new ConversationSummaryDto
            {
                Id = c.Id,
                Title = c.Title,
                UpdatedAt = c.UpdatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ConversationDetailDto?> GetConversationAsync(
        string userId,
        int id,
        CancellationToken cancellationToken = default)
    {
        var conversation = await _context.AIConversations
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, cancellationToken);
        if (conversation is null)
            return null;

        return new ConversationDetailDto
        {
            Id = conversation.Id,
            Title = conversation.Title,
            CreatedAt = conversation.CreatedAt,
            UpdatedAt = conversation.UpdatedAt,
            Messages = Deserialize(conversation.Messages)
        };
    }

    private async Task<AIConversation> GetOrCreateConversationAsync(
        string userId,
        ChatRequest request,
        CancellationToken cancellationToken)
    {
        if (request.ConversationId is > 0)
        {
            var existing = await _context.AIConversations
                .FirstOrDefaultAsync(c => c.Id == request.ConversationId && c.UserId == userId, cancellationToken);
            if (existing is null)
                throw new KeyNotFoundException("Conversation not found.");
            return existing;
        }

        var created = new AIConversation
        {
            UserId = userId,
            Title = "New conversation",
            Messages = "[]",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.AIConversations.Add(created);
        await _context.SaveChangesAsync(cancellationToken);
        return created;
    }

    private static List<ChatMessageDto> Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];
        return JsonSerializer.Deserialize<List<ChatMessageDto>>(json, JsonOptions) ?? [];
    }

    private static string TruncateTitle(string message)
    {
        var trimmed = message.Trim();
        return trimmed.Length <= 72 ? trimmed : trimmed[..69] + "...";
    }
}
