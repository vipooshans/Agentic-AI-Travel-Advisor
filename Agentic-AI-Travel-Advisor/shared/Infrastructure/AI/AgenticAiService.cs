using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TravelAdvisor.Core.Common;
using TravelAdvisor.Core.DTOs.AI;
using TravelAdvisor.Core.Entities;
using TravelAdvisor.Core.Interfaces;
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
    private readonly AgentOrchestrator _orchestrator;
    private readonly TimeProvider _clock;

    public AgenticAiService(AppDbContext context, AgentOrchestrator orchestrator, TimeProvider clock)
    {
        _context = context;
        _orchestrator = orchestrator;
        _clock = clock;
    }

    public async Task<ChatResponse> ChatAsync(UserContext caller, ChatRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            throw new ArgumentException("Message is required.");

        var conversation = await GetOrCreateConversationAsync(caller.UserId, request, cancellationToken);
        var messages = Deserialize(conversation.Messages);
        messages.Add(new ChatMessageDto { Role = "user", Content = request.Message.Trim() });

        var result = await _orchestrator.RunAsync(caller, messages, request, cancellationToken);
        var plan = result.Outcome?.Status == ChatStatus.Plan ? result.Outcome.Plan : null;
        var legacy = plan is not null ? result.Outcome!.LegacyPlan : null;
        var proposal = result.Status == ChatStatus.BookingProposal ? result.Proposal : null;

        messages.Add(new ChatMessageDto
        {
            Role = "assistant",
            Content = result.Reply,
            Status = result.Status,
            SuggestedPlan = legacy,
            Plan = plan,
            PendingBooking = proposal,
            BookingId = result.Booking?.Id
        });

        var now = _clock.GetUtcNow().UtcDateTime;
        if (string.IsNullOrWhiteSpace(conversation.Title) || conversation.Title == "New conversation")
            conversation.Title = TruncateTitle(request.Message);

        conversation.Messages = JsonSerializer.Serialize(messages, JsonOptions);
        conversation.UpdatedAt = now;

        if (plan is not null)
        {
            foreach (var record in result.Outcome!.Recommendations)
            {
                _context.AIRecommendations.Add(new AIRecommendation
                {
                    ConversationId = conversation.Id,
                    UserId = caller.UserId,
                    ItemType = record.Type,
                    HotelId = record.HotelId,
                    RoomId = record.RoomId,
                    TravelPackageId = record.TravelPackageId,
                    TransportationId = record.TransportationId,
                    DestinationId = record.DestinationId,
                    Title = record.Title,
                    EstimatedCost = record.EstimatedCost,
                    Score = record.Score,
                    Reason = record.Reason,
                    CreatedAt = now
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new ChatResponse
        {
            ConversationId = conversation.Id,
            Message = result.Reply,
            Status = result.Status,
            SuggestedPlan = legacy,
            Plan = plan,
            PendingBooking = proposal,
            Booking = result.Booking,
            Mode = result.Mode,
            Agents = result.Agents,
            ToolCalls = result.ToolCalls
        };
    }

    public async Task<List<ConversationSummaryDto>> ListConversationsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.AIConversations
            .AsNoTracking()
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
            .AsNoTracking()
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

    public async Task<List<AiRecommendationDto>> ListRecommendationsAsync(
        string userId,
        int? conversationId,
        CancellationToken cancellationToken = default)
    {
        var query = _context.AIRecommendations.AsNoTracking().Where(r => r.UserId == userId);
        if (conversationId.HasValue)
            query = query.Where(r => r.ConversationId == conversationId.Value);

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .ThenByDescending(r => r.Score)
            .Take(200)
            .Select(r => new AiRecommendationDto
            {
                Id = r.Id,
                ConversationId = r.ConversationId,
                ItemType = r.ItemType,
                HotelId = r.HotelId,
                RoomId = r.RoomId,
                TravelPackageId = r.TravelPackageId,
                TransportationId = r.TransportationId,
                DestinationId = r.DestinationId,
                Title = r.Title,
                EstimatedCost = r.EstimatedCost,
                Score = r.Score,
                Reason = r.Reason,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync(cancellationToken);
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

        var now = _clock.GetUtcNow().UtcDateTime;
        var created = new AIConversation
        {
            UserId = userId,
            Title = "New conversation",
            Messages = "[]",
            CreatedAt = now,
            UpdatedAt = now
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
