using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TravelAdvisor.Core.Common;
using TravelAdvisor.Core.DTOs.Bookings;
using TravelAdvisor.Core.DTOs.Hotels;
using TravelAdvisor.Core.DTOs.Packages;
using TravelAdvisor.Core.Entities;
using TravelAdvisor.Core.Enums;
using TravelAdvisor.Core.Interfaces;
using TravelAdvisor.Core.Interfaces.Services;
using TravelAdvisor.Infrastructure.Data;

namespace TravelAdvisor.Infrastructure.AI;

public class BookingAgentService : IBookingAgentService
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static readonly Regex CancelRegex = new(@"\b(?:cancel|drop|terminate)\b.*?(?:booking|reservation|trip)?\s*#?\s*(\d+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex InsightsRegex = new(@"\b(?:my\s*bookings|my\s*reservations|my\s*trips|insights|upcoming|status)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex GuestsRegex = new(@"\b(\d+)\s*(?:guests?|people|travelers?|pax|persons?)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex DateRangeIso = new(@"(\d{4}-\d{2}-\d{2})\s*(?:to|until|-|through)\s*(\d{4}-\d{2}-\d{2})", RegexOptions.IgnoreCase);

    private static readonly List<LlmToolDefinition> AgentTools =
    [
        new LlmToolDefinition
        {
            Name = "search_hotels",
            Description = "Search for verified hotels in Sri Lanka matching a location, dates, and number of guests.",
            ParametersSchema = """
            {
              "type": "object",
              "properties": {
                "location": { "type": "string", "description": "City or destination in Sri Lanka (e.g. Galle, Ella, Kandy, Colombo, Sigiriya, Mirissa, Nuwara Eliya)" },
                "check_in": { "type": "string", "description": "Check-in date in YYYY-MM-DD format (optional)" },
                "check_out": { "type": "string", "description": "Check-out date in YYYY-MM-DD format (optional)" },
                "guests": { "type": "integer", "description": "Number of guests (optional, default 1)" }
              },
              "required": ["location"]
            }
            """
        },
        new LlmToolDefinition
        {
            Name = "get_hotel_details",
            Description = "Retrieve detailed information, room types, pricing, and availability for a specific hotel by ID.",
            ParametersSchema = """
            {
              "type": "object",
              "properties": {
                "hotel_id": { "type": "integer", "description": "The unique hotel ID" }
              },
              "required": ["hotel_id"]
            }
            """
        },
        new LlmToolDefinition
        {
            Name = "create_booking",
            Description = "Create a confirmed hotel room reservation for the traveler once they have explicitly confirmed all details (hotel, check-in, check-out, guests). Only call this after the user says yes to a clear confirmation question.",
            ParametersSchema = """
            {
              "type": "object",
              "properties": {
                "hotel_id": { "type": "integer", "description": "The unique hotel ID to book" },
                "room_id": { "type": "integer", "description": "Optional specific room ID within the hotel" },
                "check_in": { "type": "string", "description": "Check-in date in YYYY-MM-DD format" },
                "check_out": { "type": "string", "description": "Check-out date in YYYY-MM-DD format" },
                "guests": { "type": "integer", "description": "Number of guests" }
              },
              "required": ["hotel_id", "check_in", "check_out", "guests"]
            }
            """
        }
    ];

    private readonly AppDbContext? _context;
    private readonly IBookingService _bookingService;
    private readonly IHotelService _hotelService;
    private readonly IPackageService _packageService;
    private readonly ISystemSettingsService _settingsService;
    private readonly ILlmClient _llmClient;
    private readonly TimeProvider _clock;
    private readonly AiOptions _aiOptions;
    private readonly ILogger<BookingAgentService> _logger;

    public BookingAgentService(
        AppDbContext? context,
        IBookingService bookingService,
        IHotelService hotelService,
        IPackageService packageService,
        ISystemSettingsService settingsService,
        ILlmClient llmClient,
        TimeProvider clock,
        IOptions<AiOptions> aiOptions,
        ILogger<BookingAgentService> logger)
    {
        _context = context;
        _bookingService = bookingService;
        _hotelService = hotelService;
        _packageService = packageService;
        _settingsService = settingsService;
        _llmClient = llmClient;
        _clock = clock;
        _aiOptions = aiOptions.Value;
        _logger = logger;
    }

    public async Task<BookingAgentPromptResponse> GetActiveConversationAsync(UserContext caller, CancellationToken cancellationToken = default)
    {
        if (_context == null)
        {
            return new BookingAgentPromptResponse
            {
                ActionType = "chat",
                Message = "Hello! I am your AI Booking Concierge. How can I assist you with your travels today?",
                History = []
            };
        }

        var conv = await _context.AIConversations
            .OrderByDescending(c => c.UpdatedAt)
            .FirstOrDefaultAsync(c => c.UserId == caller.UserId && c.Title == "Booking Concierge", cancellationToken);

        var history = new List<BookingAgentMessageDto>();
        if (conv != null && !string.IsNullOrWhiteSpace(conv.Messages))
        {
            try
            {
                history = JsonSerializer.Deserialize<List<BookingAgentMessageDto>>(conv.Messages, JsonOptions) ?? [];
            }
            catch { }
        }

        return new BookingAgentPromptResponse
        {
            ConversationId = conv?.Id ?? 0,
            ActionType = "chat",
            Message = history.LastOrDefault(m => m.Role == "assistant")?.Content ?? "Hello! I am your AI Booking Concierge. Tell me where you would like to stay or what kind of trip you are planning.",
            History = history
        };
    }

    public async Task<BookingAgentPromptResponse> ProcessPromptAsync(UserContext caller, BookingAgentPromptRequest request, CancellationToken cancellationToken = default)
    {
        var currentPrompt = request.Prompt?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(currentPrompt))
        {
            return new BookingAgentPromptResponse
            {
                ActionType = "chat",
                Message = "Please tell me what you would like to book or inquire about."
            };
        }

        // 1. Load or initialize persistent conversation from DB
        AIConversation? conv = null;
        var history = new List<BookingAgentMessageDto>();

        if (_context != null)
        {
            conv = await GetOrCreateConversationAsync(caller.UserId, request.ConversationId, cancellationToken);
            if (!string.IsNullOrWhiteSpace(conv.Messages))
            {
                try
                {
                    history = JsonSerializer.Deserialize<List<BookingAgentMessageDto>>(conv.Messages, JsonOptions) ?? [];
                }
                catch { }
            }
        }

        // Add traveler message to history
        var userMessage = new BookingAgentMessageDto
        {
            Role = "user",
            Content = currentPrompt,
            Timestamp = _clock.GetUtcNow().UtcDateTime
        };
        history.Add(userMessage);

        BookingAgentPromptResponse response;

        // 2. Cancellation intent check
        var cancelMatch = CancelRegex.Match(currentPrompt);
        if (cancelMatch.Success && int.TryParse(cancelMatch.Groups[1].Value, out var cancelBookingId))
        {
            response = await HandleCancellationPreviewAsync(caller, cancelBookingId, cancellationToken);
        }
        // 3. Insights intent check
        else if (InsightsRegex.IsMatch(currentPrompt))
        {
            var insights = await GetBookingInsightsAsync(caller, cancellationToken);
            response = new BookingAgentPromptResponse
            {
                ActionType = "insights",
                Message = insights.Count == 0
                    ? "You do not have any active or upcoming reservations right now."
                    : $"You have {insights.Count} active booking update(s). Check your upcoming deadlines below.",
                Insights = insights
            };
        }
        else
        {
            try
            {
                if (_llmClient.IsConfigured)
                {
                    _logger.LogInformation("Processing booking prompt with AI Provider: {Provider}, Model: {Model}, BaseUrl: {BaseUrl}",
                        _aiOptions.Provider, _aiOptions.Model, _aiOptions.BaseUrl);
                    response = await ProcessWithLlmAsync(caller, conv, history, cancellationToken);
                }
                else
                {
                    // Fallback when LLM is not configured (e.g. offline unit testing)
                    var intent = FallbackParseIntent(currentPrompt);
                    response = await ExecuteFallbackIntentAsync(caller, intent, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to communicate with LLM for prompt '{Prompt}' (Provider: {Provider}, Model: {Model})",
                    currentPrompt, _aiOptions.Provider, _aiOptions.Model);
                response = new BookingAgentPromptResponse
                {
                    ConversationId = conv?.Id ?? 0,
                    ActionType = "chat",
                    Message = "The assistant is busy right now, please try again in a moment."
                };
            }
        }

        // Record assistant response in history
        var assistantMessage = new BookingAgentMessageDto
        {
            Role = "assistant",
            Content = response.Message,
            Proposal = response.Proposal,
            Cancellation = response.Cancellation,
            Timestamp = _clock.GetUtcNow().UtcDateTime
        };
        history.Add(assistantMessage);

        // Save conversation back to Database
        if (_context != null && conv != null)
        {
            conv.Messages = JsonSerializer.Serialize(history, JsonOptions);
            conv.UpdatedAt = _clock.GetUtcNow().UtcDateTime;
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to persist booking conversation to database.");
            }
            response.ConversationId = conv.Id;
        }

        response.History = history;
        return response;
    }

    private static List<LlmMessage> BuildAlternatingMessages(string systemInstruction, List<BookingAgentMessageDto> history)
    {
        var result = new List<LlmMessage>
        {
            new() { Role = LlmRoles.System, Content = systemInstruction }
        };

        // Filter out empty messages and error notices
        var valid = history
            .Where(m => !string.IsNullOrWhiteSpace(m.Content)
                     && !m.Content.StartsWith("The assistant is busy right now", StringComparison.OrdinalIgnoreCase)
                     && !m.Content.StartsWith("Sorry,", StringComparison.OrdinalIgnoreCase))
            .TakeLast(20)
            .ToList();

        var turns = new List<LlmMessage>();
        foreach (var msg in valid)
        {
            var role = msg.Role == "user" ? LlmRoles.User : LlmRoles.Assistant;
            var content = msg.Content.Trim();

            if (turns.Count == 0)
            {
                // First turn after system MUST be user
                if (role == LlmRoles.User)
                {
                    turns.Add(new LlmMessage { Role = role, Content = content });
                }
                continue;
            }

            var lastTurn = turns[^1];
            if (lastTurn.Role == role)
            {
                // Merge consecutive turns with the same role to strictly guarantee alternation for Gemini
                turns[^1] = new LlmMessage
                {
                    Role = role,
                    Content = $"{lastTurn.Content}\n{content}"
                };
            }
            else
            {
                turns.Add(new LlmMessage { Role = role, Content = content });
            }
        }

        result.AddRange(turns);
        return result;
    }

    private async Task<BookingAgentPromptResponse> ProcessWithLlmAsync(
        UserContext caller,
        AIConversation? conv,
        List<BookingAgentMessageDto> history,
        CancellationToken cancellationToken)
    {
        var systemInstruction = "You are a friendly, natural travel assistant for Travel Advisor, a Sri Lanka hotel and tour booking platform. Chat like a real person. Reply to greetings and small talk naturally and briefly (e.g. 'hi' -> 'Hey! How\\'s it going?'). Always respond to what the user actually said and never ignore it. Your job is to help with hotel stays, tour packages and destinations in Sri Lanka, but don't push it. After a bit of conversation, or when the user mentions travel, gently guide toward where they want to go, their dates and number of guests. Ask for only ONE missing detail at a time. Never repeat the same question if the user said something else. Never invent hotels, prices or availability; only use data returned by the tools. Keep replies short (1-3 sentences). Use plain text, no markdown, no asterisks.";

        var llmMessages = BuildAlternatingMessages(systemInstruction, history);

        string finalReply = string.Empty;
        var maxIterations = Math.Max(3, _aiOptions.MaxToolIterations);

        for (var i = 0; i < maxIterations; i++)
        {
            var llmResponse = await _llmClient.ChatWithToolsAsync(llmMessages, AgentTools, cancellationToken);

            if (llmResponse.ToolCalls.Count == 0)
            {
                finalReply = llmResponse.Content ?? string.Empty;
                break;
            }

            // LLM wants to execute tool calls
            llmMessages.Add(new LlmMessage
            {
                Role = LlmRoles.Assistant,
                Content = llmResponse.Content ?? string.Empty,
                ToolCalls = llmResponse.ToolCalls
            });

            foreach (var toolCall in llmResponse.ToolCalls)
            {
                _logger.LogInformation("LLM calling tool {ToolName} with arguments: {Args}", toolCall.Name, toolCall.ArgumentsJson);
                var toolResult = toolCall.Name switch
                {
                    "search_hotels" => await ExecuteSearchHotelsAsync(caller, toolCall.ArgumentsJson, cancellationToken),
                    "get_hotel_details" => await ExecuteGetHotelDetailsAsync(caller, toolCall.ArgumentsJson, cancellationToken),
                    "create_booking" => await ExecuteCreateBookingAsync(caller, toolCall.ArgumentsJson, cancellationToken),
                    _ => JsonSerializer.Serialize(new { error = $"Unknown tool '{toolCall.Name}'" })
                };

                llmMessages.Add(new LlmMessage
                {
                    Role = LlmRoles.Tool,
                    ToolCallId = toolCall.Id,
                    Content = toolResult
                });
            }
        }

        finalReply = CleanPlainText(finalReply);
        if (string.IsNullOrWhiteSpace(finalReply))
        {
            finalReply = "I am here to help you explore and book stays in Sri Lanka. Where are you planning to visit?";
        }

        return new BookingAgentPromptResponse
        {
            ConversationId = conv?.Id ?? 0,
            ActionType = "chat",
            Message = finalReply
        };
    }

    private async Task<string> ExecuteSearchHotelsAsync(UserContext caller, string argumentsJson, CancellationToken cancellationToken)
    {
        try
        {
            using var doc = JsonDocument.Parse(argumentsJson);
            var root = doc.RootElement;
            var location = root.TryGetProperty("location", out var loc) ? loc.GetString() : null;
            var guests = root.TryGetProperty("guests", out var g) && g.TryGetInt32(out var gi) ? gi : 1;

            var results = await _hotelService.SearchAsync(caller, new HotelSearchQuery
            {
                City = location,
                Q = location,
                Guests = guests > 0 ? guests : 1
            }, cancellationToken);

            if (results.Count == 0 && !string.IsNullOrWhiteSpace(location))
            {
                var all = await _hotelService.SearchAsync(caller, new HotelSearchQuery(), cancellationToken);
                results = all.Where(h =>
                    (h.City != null && h.City.Contains(location, StringComparison.OrdinalIgnoreCase)) ||
                    (h.Name != null && h.Name.Contains(location, StringComparison.OrdinalIgnoreCase)) ||
                    (h.Address != null && h.Address.Contains(location, StringComparison.OrdinalIgnoreCase))
                ).ToList();
            }

            var list = results.Take(5).Select(h => new
            {
                hotel_id = h.Id,
                name = h.Name,
                city = h.City,
                address = h.Address,
                rating = h.AverageRating,
                min_price_per_night = h.MinPricePerNight,
                description = h.Description
            }).ToList();

            return JsonSerializer.Serialize(list, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Tool search_hotels execution error");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    private async Task<string> ExecuteGetHotelDetailsAsync(UserContext caller, string argumentsJson, CancellationToken cancellationToken)
    {
        try
        {
            using var doc = JsonDocument.Parse(argumentsJson);
            var root = doc.RootElement;
            if (!root.TryGetProperty("hotel_id", out var idProp) || !idProp.TryGetInt32(out var hotelId))
            {
                return JsonSerializer.Serialize(new { error = "hotel_id is required" });
            }

            var hotel = await _hotelService.GetAsync(caller, hotelId, cancellationToken);
            var summary = new
            {
                hotel_id = hotel.Id,
                name = hotel.Name,
                city = hotel.City,
                description = hotel.Description,
                rooms = hotel.Rooms.Select(r => new
                {
                    room_id = r.Id,
                    name = r.Name,
                    room_type = r.RoomType,
                    capacity = r.Capacity,
                    price_per_night = r.PricePerNight,
                    is_available = r.IsAvailable
                }).ToList()
            };

            return JsonSerializer.Serialize(summary, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Tool get_hotel_details execution error");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    private async Task<string> ExecuteCreateBookingAsync(UserContext caller, string argumentsJson, CancellationToken cancellationToken)
    {
        try
        {
            using var doc = JsonDocument.Parse(argumentsJson);
            var root = doc.RootElement;

            if (!root.TryGetProperty("hotel_id", out var hidProp) || !hidProp.TryGetInt32(out var hotelId))
                return JsonSerializer.Serialize(new { success = false, error = "hotel_id is required" });

            if (!root.TryGetProperty("check_in", out var ciProp) || !DateTime.TryParse(ciProp.GetString(), Inv, DateTimeStyles.AssumeUniversal, out var checkIn))
                return JsonSerializer.Serialize(new { success = false, error = "valid check_in date (YYYY-MM-DD) is required" });

            if (!root.TryGetProperty("check_out", out var coProp) || !DateTime.TryParse(coProp.GetString(), Inv, DateTimeStyles.AssumeUniversal, out var checkOut))
                return JsonSerializer.Serialize(new { success = false, error = "valid check_out date (YYYY-MM-DD) is required" });

            var guests = root.TryGetProperty("guests", out var gProp) && gProp.TryGetInt32(out var g) ? g : 1;

            int roomId = 0;
            if (root.TryGetProperty("room_id", out var ridProp) && ridProp.TryGetInt32(out var rid) && rid > 0)
            {
                roomId = rid;
            }
            else
            {
                var hotel = await _hotelService.GetAsync(caller, hotelId, cancellationToken);
                var room = hotel.Rooms.FirstOrDefault(r => r.IsAvailable && r.Capacity >= guests) ?? hotel.Rooms.FirstOrDefault();
                if (room == null)
                    return JsonSerializer.Serialize(new { success = false, error = "No rooms available at this hotel." });
                roomId = room.Id;
            }

            var booking = await _bookingService.CreateAsync(caller, new CreateBookingRequest
            {
                RoomId = roomId,
                CheckIn = checkIn,
                CheckOut = checkOut,
                Guests = guests
            }, cancellationToken);

            return JsonSerializer.Serialize(new
            {
                success = true,
                booking_id = booking.Id,
                hotel_name = booking.HotelName,
                room_name = booking.RoomName,
                check_in = booking.CheckIn.ToString("yyyy-MM-dd"),
                check_out = booking.CheckOut.ToString("yyyy-MM-dd"),
                guests = booking.Guests,
                total_price = booking.TotalPrice,
                status = booking.Status.ToString()
            }, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Tool create_booking execution error");
            return JsonSerializer.Serialize(new { success = false, error = ex.Message });
        }
    }

    private static string CleanPlainText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        var cleaned = text.Replace("**", "").Replace("*", "");
        cleaned = Regex.Replace(cleaned, @"^#+\s*", "", RegexOptions.Multiline);
        return cleaned.Trim();
    }

    public async Task<BookingDto> ConfirmProposalAsync(UserContext caller, ConfirmBookingAgentRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ProposalToken))
            throw new BusinessRuleException("A valid proposal token is required to confirm a booking.");

        if (!ValidateProposalToken(request.ProposalToken, out var tokenData, out var error))
            throw new BusinessRuleException($"Invalid or expired booking proposal: {error}");

        var createRequest = new CreateBookingRequest
        {
            RoomId = tokenData.RoomId ?? request.RoomId,
            TravelPackageId = tokenData.PackageId ?? request.TravelPackageId,
            CheckIn = tokenData.CheckIn,
            CheckOut = tokenData.CheckOut ?? request.CheckOut,
            Guests = tokenData.Guests > 0 ? tokenData.Guests : request.Guests,
            Notes = request.Notes
        };

        return await _bookingService.CreateAsync(caller, createRequest, cancellationToken);
    }

    public async Task<List<BookingInsightDto>> GetBookingInsightsAsync(UserContext caller, CancellationToken cancellationToken = default)
    {
        var bookings = await _bookingService.ListAsync(caller, cancellationToken);
        var now = _clock.GetUtcNow().UtcDateTime;
        var cutoffHours = 48;

        try
        {
            cutoffHours = (await _settingsService.GetPublicAsync(cancellationToken)).GuestCancellationCutoffHours;
        }
        catch { }

        var insights = new List<BookingInsightDto>();

        foreach (var b in bookings.Where(b => b.Status != BookingStatus.Cancelled && b.Status != BookingStatus.Completed))
        {
            var listingName = b.RoomName != null ? $"{b.RoomName} ({b.HotelName})" : (b.PackageTitle ?? $"Booking #{b.Id}");
            var daysUntil = (b.CheckIn.Date - now.Date).TotalDays;

            if (b.Status == BookingStatus.Pending)
            {
                insights.Add(new BookingInsightDto
                {
                    BookingId = b.Id,
                    Type = "advisory",
                    Title = "Pending Provider Confirmation",
                    Message = $"Your reservation #{b.Id} for {listingName} is awaiting confirmation from the property.",
                    ActionUrl = $"/Customer/Bookings",
                    ActionLabel = "View Status"
                });
            }

            if (b.Status == BookingStatus.Confirmed)
            {
                var hoursUntilCheckIn = (b.CheckIn - now).TotalHours;
                var hoursUntilCutoff = hoursUntilCheckIn - cutoffHours;

                if (hoursUntilCutoff > 0 && hoursUntilCutoff <= 48)
                {
                    insights.Add(new BookingInsightDto
                    {
                        BookingId = b.Id,
                        Type = "urgent",
                        Title = "Free Cancellation Ending Soon",
                        Message = $"Free cancellation for {listingName} closes in {(int)hoursUntilCutoff} hour(s) (Cutoff: {b.CheckIn.AddHours(-cutoffHours):MMM dd, HH:mm} UTC).",
                        ActionUrl = $"/Customer/Bookings",
                        ActionLabel = "Manage Booking"
                    });
                }
                else if (daysUntil is >= 0 and <= 3)
                {
                    insights.Add(new BookingInsightDto
                    {
                        BookingId = b.Id,
                        Type = "tip",
                        Title = "Upcoming Trip in " + (int)daysUntil + " Day(s)",
                        Message = $"Check-in for {listingName} begins on {b.CheckIn:MMMM dd, yyyy}. Pack light and keep your booking #{b.Id} ready.",
                        ActionUrl = $"/Customer/Bookings",
                        ActionLabel = "Trip Voucher"
                    });
                }
            }
        }

        return insights;
    }

    private async Task<AIConversation> GetOrCreateConversationAsync(string userId, int? conversationId, CancellationToken cancellationToken)
    {
        if (_context == null) throw new InvalidOperationException("DbContext is not available.");

        AIConversation? conv = null;
        if (conversationId.HasValue && conversationId.Value > 0)
        {
            conv = await _context.AIConversations.FirstOrDefaultAsync(c => c.Id == conversationId.Value && c.UserId == userId, cancellationToken);
        }

        if (conv == null)
        {
            conv = new AIConversation
            {
                UserId = userId,
                Title = "Booking Concierge",
                Messages = "[]",
                CreatedAt = _clock.GetUtcNow().UtcDateTime,
                UpdatedAt = _clock.GetUtcNow().UtcDateTime
            };
            _context.AIConversations.Add(conv);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return conv;
    }

    private async Task<BookingAgentPromptResponse> HandleCancellationPreviewAsync(UserContext caller, int bookingId, CancellationToken cancellationToken)
    {
        try
        {
            var booking = await _bookingService.GetAsync(caller, bookingId, cancellationToken);
            var now = _clock.GetUtcNow().UtcDateTime;
            var cutoffHours = (await _settingsService.GetPublicAsync(cancellationToken)).GuestCancellationCutoffHours;

            var isBeforeCutoff = (booking.CheckIn - now).TotalHours >= cutoffHours;
            var canCancel = (booking.Status == BookingStatus.Pending) || (booking.Status == BookingStatus.Confirmed && isBeforeCutoff);
            var listingName = booking.RoomName != null ? $"{booking.RoomName} ({booking.HotelName})" : (booking.PackageTitle ?? $"Booking #{booking.Id}");

            var message = canCancel
                ? $"Are you sure you want to cancel your reservation #{booking.Id} for {listingName}? Free cancellation deadline is {booking.CheckIn.AddHours(-cutoffHours):MMM dd, yyyy HH:mm} UTC."
                : $"Booking #{booking.Id} cannot be cancelled online because it is within {cutoffHours} hours of check-in.";

            return new BookingAgentPromptResponse
            {
                ActionType = "cancellation",
                Message = message,
                Cancellation = new CancellationPreviewDto
                {
                    BookingId = booking.Id,
                    ListingName = listingName,
                    CheckIn = booking.CheckIn,
                    CheckOut = booking.CheckOut,
                    TotalPrice = booking.TotalPrice,
                    Status = booking.Status,
                    CanCancel = canCancel,
                    Message = message
                }
            };
        }
        catch (NotFoundException)
        {
            return new BookingAgentPromptResponse
            {
                ActionType = "chat",
                Message = $"I could not find booking #{bookingId} associated with your account."
            };
        }
    }

    private static ParsedFallbackIntent FallbackParseIntent(string prompt)
    {
        var intent = new ParsedFallbackIntent();

        var dateMatch = DateRangeIso.Match(prompt);
        if (dateMatch.Success &&
            DateTime.TryParse(dateMatch.Groups[1].Value, Inv, DateTimeStyles.AssumeUniversal, out var inDt) &&
            DateTime.TryParse(dateMatch.Groups[2].Value, Inv, DateTimeStyles.AssumeUniversal, out var outDt))
        {
            intent.CheckIn = inDt;
            intent.CheckOut = outDt;
        }

        var guestsMatch = GuestsRegex.Match(prompt);
        if (guestsMatch.Success && int.TryParse(guestsMatch.Groups[1].Value, out var g))
        {
            intent.Guests = g;
        }

        var destinations = new[] { "ella", "galle", "kandy", "colombo", "sigiriya", "mirissa", "nuwara eliya", "bentota", "yala" };
        foreach (var d in destinations)
        {
            if (prompt.Contains(d, StringComparison.OrdinalIgnoreCase))
            {
                intent.Location = d;
                break;
            }
        }

        intent.HasBookingKeyword = prompt.Contains("book", StringComparison.OrdinalIgnoreCase) ||
                                  prompt.Contains("hotel", StringComparison.OrdinalIgnoreCase) ||
                                  prompt.Contains("room", StringComparison.OrdinalIgnoreCase);

        return intent;
    }

    private async Task<BookingAgentPromptResponse> ExecuteFallbackIntentAsync(UserContext caller, ParsedFallbackIntent intent, CancellationToken cancellationToken)
    {
        if (intent.HasBookingKeyword && string.IsNullOrWhiteSpace(intent.Location) && !intent.CheckIn.HasValue)
        {
            return new BookingAgentPromptResponse
            {
                ActionType = "info",
                Message = "Could you please specify your preferred hotel or destination, check-in date, check-out date, and the number of guests?"
            };
        }

        if (intent.Location != null && intent.CheckIn.HasValue && intent.CheckOut.HasValue)
        {
            var hotels = await _hotelService.SearchAsync(caller, new HotelSearchQuery
            {
                City = intent.Location,
                Q = intent.Location,
                Guests = intent.Guests
            }, cancellationToken);

            var hotel = hotels.FirstOrDefault();
            if (hotel != null)
            {
                var detail = await _hotelService.GetAsync(caller, hotel.Id, cancellationToken);
                var room = detail.Rooms.FirstOrDefault(r => r.IsAvailable && r.Capacity >= intent.Guests) ?? detail.Rooms.FirstOrDefault();

                if (room != null)
                {
                    var quote = await _bookingService.CheckAvailabilityAsync(caller, new AvailabilityQuery
                    {
                        RoomId = room.Id,
                        CheckIn = intent.CheckIn.Value,
                        CheckOut = intent.CheckOut.Value,
                        Guests = intent.Guests
                    }, cancellationToken);

                    if (quote.Available)
                    {
                        var expiresAt = _clock.GetUtcNow().UtcDateTime.AddMinutes(Math.Max(5, _aiOptions.BookingProposalMinutes));
                        var token = GenerateProposalToken(room.Id, null, quote.CheckIn, quote.CheckOut, quote.Guests, quote.TotalPrice, expiresAt);

                        return new BookingAgentPromptResponse
                        {
                            ActionType = "proposal",
                            Message = $"I found a stay at {hotel.Name} ({room.Name}) from {quote.CheckIn:MMM dd, yyyy} to {quote.CheckOut:MMM dd, yyyy} for LKR {quote.TotalPrice:N0}.",
                            Proposal = new BookingProposalDto
                            {
                                Kind = "hotel",
                                RoomId = room.Id,
                                HotelName = hotel.Name,
                                RoomName = room.Name,
                                CheckIn = quote.CheckIn,
                                CheckOut = quote.CheckOut,
                                Nights = quote.Nights,
                                Guests = quote.Guests,
                                TotalPrice = quote.TotalPrice,
                                ProposalToken = token,
                                ExpiresAt = expiresAt
                            }
                        };
                    }
                }
            }
        }

        return new BookingAgentPromptResponse
        {
            ActionType = "chat",
            Message = "How can I assist you with your travels in Sri Lanka today?"
        };
    }

    private static string GenerateProposalToken(int? roomId, int? packageId, DateTime checkIn, DateTime? checkOut, int guests, decimal price, DateTime expiresAt)
    {
        var payload = $"{roomId}:{packageId}:{checkIn:O}:{checkOut:O}:{guests}:{price:F2}:{expiresAt:O}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));
    }

    private bool ValidateProposalToken(string token, out TokenData data, out string error)
    {
        data = new TokenData();
        error = string.Empty;

        try
        {
            var bytes = Convert.FromBase64String(token);
            var str = Encoding.UTF8.GetString(bytes);
            var parts = str.Split(':');
            if (parts.Length < 7)
            {
                error = "Malformed proposal signature.";
                return false;
            }

            data.RoomId = int.TryParse(parts[0], out var r) ? r : null;
            data.PackageId = int.TryParse(parts[1], out var p) ? p : null;
            data.CheckIn = DateTime.Parse(parts[2], Inv, DateTimeStyles.RoundtripKind);
            data.CheckOut = string.IsNullOrEmpty(parts[3]) ? null : DateTime.Parse(parts[3], Inv, DateTimeStyles.RoundtripKind);
            data.Guests = int.Parse(parts[4], Inv);
            data.Price = decimal.Parse(parts[5], Inv);
            data.ExpiresAt = DateTime.Parse(parts[6], Inv, DateTimeStyles.RoundtripKind);

            if (_clock.GetUtcNow().UtcDateTime > data.ExpiresAt)
            {
                error = "Proposal has expired. Please request a new quote.";
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private sealed class TokenData
    {
        public int? RoomId { get; set; }
        public int? PackageId { get; set; }
        public DateTime CheckIn { get; set; }
        public DateTime? CheckOut { get; set; }
        public int Guests { get; set; }
        public decimal Price { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    private sealed class ParsedFallbackIntent
    {
        public string? Location { get; set; }
        public DateTime? CheckIn { get; set; }
        public DateTime? CheckOut { get; set; }
        public int Guests { get; set; } = 2;
        public bool HasBookingKeyword { get; set; }
    }
}
