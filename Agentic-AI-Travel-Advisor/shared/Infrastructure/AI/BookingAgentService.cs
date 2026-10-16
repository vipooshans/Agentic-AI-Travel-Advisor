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

    private static readonly Regex CancelRegex = new(@"^\s*(?:cancel|drop|terminate)\b.*?(?:booking|reservation|trip)?\s*#?\s*(\d+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex GuestsRegex = new(@"\b(\d+)\s*(?:guests?|people|travelers?|pax|persons?)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex DateRangeIso = new(@"(\d{4}-\d{2}-\d{2})\s*(?:to|until|-|through)\s*(\d{4}-\d{2}-\d{2})", RegexOptions.IgnoreCase);

    private static readonly List<LlmToolDefinition> AgentTools =
    [
        new LlmToolDefinition
        {
            Name = "search_hotels",
            Description = "Search for verified hotels in Sri Lanka matching a location, city, dates, or guests.",
            ParametersSchema = """
            {
              "type": "object",
              "properties": {
                "location": { "type": "string", "description": "City or destination in Sri Lanka (e.g. Galle, Ella, Kandy, Colombo, Sigiriya, Mirissa, Nuwara Eliya, Bentota, Yala)" },
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
            Description = "Retrieve detailed information, rooms, pricing, and availability for a specific hotel by ID.",
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
            Name = "search_packages",
            Description = "Search for tour packages, itineraries, and experiences in Sri Lanka.",
            ParametersSchema = """
            {
              "type": "object",
              "properties": {
                "query": { "type": "string", "description": "Theme, activity or keyword (e.g. cultural, safari, hill country, beach, train)" },
                "location": { "type": "string", "description": "Destination or city in Sri Lanka (optional)" }
              }
            }
            """
        },
        new LlmToolDefinition
        {
            Name = "get_my_bookings",
            Description = "Retrieve the traveler's active and upcoming hotel and tour package reservations.",
            ParametersSchema = """
            {
              "type": "object",
              "properties": {}
            }
            """
        },
        new LlmToolDefinition
        {
            Name = "create_booking",
            Description = "Directly create and confirm a reservation for a hotel stay or tour package in Sri Lanka.",
            ParametersSchema = """
            {
              "type": "object",
              "properties": {
                "hotel_id": { "type": "integer", "description": "The hotel ID (required for hotel stays)" },
                "room_id": { "type": "integer", "description": "Optional specific room ID within the hotel" },
                "package_id": { "type": "integer", "description": "The tour package ID (required for tour packages)" },
                "check_in": { "type": "string", "description": "Check-in date in YYYY-MM-DD format" },
                "check_out": { "type": "string", "description": "Check-out date in YYYY-MM-DD format (optional for tour packages)" },
                "guests": { "type": "integer", "description": "Number of guests" },
                "notes": { "type": "string", "description": "Optional traveler notes or special requests" }
              },
              "required": ["check_in", "guests"]
            }
            """
        },
        new LlmToolDefinition
        {
            Name = "create_proposal",
            Description = "Generate an interactive price quote and proposal card with a 'Review & Reserve' button for the traveler.",
            ParametersSchema = """
            {
              "type": "object",
              "properties": {
                "hotel_id": { "type": "integer", "description": "The hotel ID (for hotel stays)" },
                "room_id": { "type": "integer", "description": "Optional room ID" },
                "package_id": { "type": "integer", "description": "The tour package ID (for tour packages)" },
                "check_in": { "type": "string", "description": "Check-in date in YYYY-MM-DD format" },
                "check_out": { "type": "string", "description": "Check-out date in YYYY-MM-DD format" },
                "guests": { "type": "integer", "description": "Number of guests" }
              },
              "required": ["check_in", "guests"]
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

        // 2. Cancellation intent check for explicit commands (e.g. "cancel booking #12" or "cancel #12")
        var cancelMatch = CancelRegex.Match(currentPrompt);
        if (cancelMatch.Success && int.TryParse(cancelMatch.Groups[1].Value, out var cancelBookingId))
        {
            response = await HandleCancellationPreviewAsync(caller, cancelBookingId, cancellationToken);
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
            Booking = response.Booking,
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
        var systemInstruction = """
You are a friendly, natural, and helpful AI Travel Assistant for Travel Advisor, a premier hotel and tour booking platform in Sri Lanka.
Talk just like ChatGPT: casual, warm, engaging, and conversational.

Conversation Style:
1. Talk naturally like a real person:
   - Always respond directly to what the user actually said.
   - For greetings ("hi", "hello"), casual check-ins ("how are you", "did you eat"), jokes, food questions, or general chat, reply warmly and naturally (e.g. "I'm doing great, thank you! How are you doing today?").
   - You can chat freely about Sri Lanka: cities (Galle, Kandy, Colombo, Ella, Sigiriya, Mirissa, Nuwara Eliya), attractions, train rides, beaches, wildlife safaris, and famous food (kottu, egg hoppers, Ceylon tea).
   - Do NOT push hotel bookings aggressively on casual small talk or greetings. Talk like a friendly human first.

2. Hotel & tour recommendations:
   - When the user asks about hotels, destinations, or trips, give genuine, helpful recommendations.
   - When they want to plan or book a stay, guide them smoothly: find out where they want to go, check-in and check-out dates, and number of guests.
   - Always call your tools (search_hotels, get_hotel_details, search_packages, get_my_bookings) to look up real, verified listings. Never invent fake hotels or prices.

3. Booking execution:
   - When the user confirms they want to book a hotel or tour package, use your create_booking tool to make the reservation.
   - If the traveler prefers a price quote with a confirmation button to review first, use create_proposal.
   - Once booked, share their confirmed booking ID, dates, and total price in LKR.

4. Existing bookings:
   - If the user asks about their reservations, active trips, or booking status, use get_my_bookings.

5. Tone & Formatting:
   - Friendly, polite, and helpful. Use readable markdown with bold text (**Hotel Name**, dates, and prices).
""";

        var llmMessages = BuildAlternatingMessages(systemInstruction, history);

        string finalReply = string.Empty;
        var maxIterations = Math.Max(3, _aiOptions.MaxToolIterations);
        BookingDto? lastCreatedBooking = null;
        BookingProposalDto? lastCreatedProposal = null;

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
                string toolResult;
                switch (toolCall.Name)
                {
                    case "search_hotels":
                        toolResult = await ExecuteSearchHotelsAsync(caller, toolCall.ArgumentsJson, cancellationToken);
                        break;
                    case "get_hotel_details":
                        toolResult = await ExecuteGetHotelDetailsAsync(caller, toolCall.ArgumentsJson, cancellationToken);
                        break;
                    case "search_packages":
                        toolResult = await ExecuteSearchPackagesAsync(toolCall.ArgumentsJson, cancellationToken);
                        break;
                    case "get_my_bookings":
                        toolResult = await ExecuteGetMyBookingsAsync(caller, cancellationToken);
                        break;
                    case "create_booking":
                        var bookingOutcome = await ExecuteCreateBookingAsync(caller, toolCall.ArgumentsJson, cancellationToken);
                        toolResult = bookingOutcome.Json;
                        if (bookingOutcome.Booking != null)
                            lastCreatedBooking = bookingOutcome.Booking;
                        break;
                    case "create_proposal":
                        var proposalOutcome = await ExecuteCreateProposalAsync(caller, toolCall.ArgumentsJson, cancellationToken);
                        toolResult = proposalOutcome.Json;
                        if (proposalOutcome.Proposal != null)
                            lastCreatedProposal = proposalOutcome.Proposal;
                        break;
                    default:
                        toolResult = JsonSerializer.Serialize(new { error = $"Unknown tool '{toolCall.Name}'" });
                        break;
                }

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

        var actionType = "chat";
        if (lastCreatedBooking != null)
            actionType = "booking_confirmed";
        else if (lastCreatedProposal != null)
            actionType = "proposal";

        return new BookingAgentPromptResponse
        {
            ConversationId = conv?.Id ?? 0,
            ActionType = actionType,
            Message = finalReply,
            Booking = lastCreatedBooking,
            Proposal = lastCreatedProposal
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

    private async Task<string> ExecuteSearchPackagesAsync(string argumentsJson, CancellationToken cancellationToken)
    {
        try
        {
            if (_context == null)
                return JsonSerializer.Serialize(new { error = "Database context not available" });

            string? query = null;
            string? location = null;
            if (!string.IsNullOrWhiteSpace(argumentsJson))
            {
                using var doc = JsonDocument.Parse(argumentsJson);
                var root = doc.RootElement;
                if (root.TryGetProperty("query", out var q)) query = q.GetString();
                if (root.TryGetProperty("location", out var loc)) location = loc.GetString();
            }

            var queryable = _context.TravelPackages
                .AsNoTracking()
                .Include(p => p.Destination)
                .Where(p => p.ApprovalStatus == ApprovalStatus.Approved);

            if (!string.IsNullOrWhiteSpace(location))
            {
                var term = location.Trim();
                queryable = queryable.Where(p => EF.Functions.ILike(p.Destination.Name, $"%{term}%") || EF.Functions.ILike(p.Destination.City, $"%{term}%"));
            }

            if (!string.IsNullOrWhiteSpace(query))
            {
                var term = query.Trim();
                queryable = queryable.Where(p =>
                    EF.Functions.ILike(p.Title, $"%{term}%") ||
                    (p.Description != null && EF.Functions.ILike(p.Description, $"%{term}%")));
            }

            var packages = await queryable.OrderBy(p => p.Price).Take(5).Select(p => new
            {
                package_id = p.Id,
                title = p.Title,
                destination = p.Destination.Name,
                price = p.Price,
                duration_days = p.DurationDays,
                max_travelers = p.MaxTravelers,
                description = p.Description
            }).ToListAsync(cancellationToken);

            return JsonSerializer.Serialize(packages, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Tool search_packages execution error");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    private async Task<string> ExecuteGetMyBookingsAsync(UserContext caller, CancellationToken cancellationToken)
    {
        try
        {
            var bookings = await _bookingService.ListAsync(caller, cancellationToken);
            var list = bookings.OrderByDescending(b => b.CreatedAt).Take(5).Select(b => new
            {
                booking_id = b.Id,
                listing_name = b.RoomName != null ? $"{b.HotelName} ({b.RoomName})" : (b.PackageTitle ?? $"Booking #{b.Id}"),
                check_in = b.CheckIn.ToString("yyyy-MM-dd"),
                check_out = b.CheckOut.ToString("yyyy-MM-dd"),
                guests = b.Guests,
                total_price = b.TotalPrice,
                status = b.Status.ToString()
            }).ToList();

            return JsonSerializer.Serialize(list, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Tool get_my_bookings execution error");
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

    private async Task<(string Json, BookingDto? Booking)> ExecuteCreateBookingAsync(UserContext caller, string argumentsJson, CancellationToken cancellationToken)
    {
        try
        {
            using var doc = JsonDocument.Parse(argumentsJson);
            var root = doc.RootElement;

            int hotelId = root.TryGetProperty("hotel_id", out var hidProp) && hidProp.TryGetInt32(out var hid) ? hid : 0;
            int roomId = root.TryGetProperty("room_id", out var ridProp) && ridProp.TryGetInt32(out var rid) ? rid : 0;
            int packageId = root.TryGetProperty("package_id", out var pidProp) && pidProp.TryGetInt32(out var pid) ? pid : 0;

            if (!root.TryGetProperty("check_in", out var ciProp) || !DateTime.TryParse(ciProp.GetString(), Inv, DateTimeStyles.AssumeUniversal, out var checkIn))
                return (JsonSerializer.Serialize(new { success = false, error = "Valid check_in date (YYYY-MM-DD) is required" }), null);

            DateTime? checkOut = null;
            if (root.TryGetProperty("check_out", out var coProp) && DateTime.TryParse(coProp.GetString(), Inv, DateTimeStyles.AssumeUniversal, out var co))
            {
                checkOut = co;
            }

            var guests = root.TryGetProperty("guests", out var gProp) && gProp.TryGetInt32(out var g) ? g : 1;
            string? notes = root.TryGetProperty("notes", out var nProp) ? nProp.GetString() : null;

            if (packageId > 0)
            {
                var packageBooking = await _bookingService.CreateAsync(caller, new CreateBookingRequest
                {
                    TravelPackageId = packageId,
                    CheckIn = checkIn,
                    CheckOut = checkOut ?? checkIn.AddDays(3),
                    Guests = guests,
                    Notes = notes
                }, cancellationToken);

                return (JsonSerializer.Serialize(new
                {
                    success = true,
                    booking_id = packageBooking.Id,
                    package_title = packageBooking.PackageTitle,
                    check_in = packageBooking.CheckIn.ToString("yyyy-MM-dd"),
                    check_out = packageBooking.CheckOut.ToString("yyyy-MM-dd"),
                    guests = packageBooking.Guests,
                    total_price = packageBooking.TotalPrice,
                    status = packageBooking.Status.ToString()
                }, JsonOptions), packageBooking);
            }

            if (hotelId <= 0 && roomId <= 0)
            {
                return (JsonSerializer.Serialize(new { success = false, error = "hotel_id or room_id is required" }), null);
            }

            if (roomId <= 0 && hotelId > 0)
            {
                var hotel = await _hotelService.GetAsync(caller, hotelId, cancellationToken);
                var room = hotel.Rooms.FirstOrDefault(r => r.IsAvailable && r.Capacity >= guests) ?? hotel.Rooms.FirstOrDefault();
                if (room == null)
                    return (JsonSerializer.Serialize(new { success = false, error = "No rooms available at this hotel." }), null);
                roomId = room.Id;
            }

            if (!checkOut.HasValue)
                checkOut = checkIn.AddDays(1);

            var booking = await _bookingService.CreateAsync(caller, new CreateBookingRequest
            {
                RoomId = roomId,
                CheckIn = checkIn,
                CheckOut = checkOut.Value,
                Guests = guests,
                Notes = notes
            }, cancellationToken);

            return (JsonSerializer.Serialize(new
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
            }, JsonOptions), booking);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Tool create_booking execution error");
            return (JsonSerializer.Serialize(new { success = false, error = ex.Message }), null);
        }
    }

    private async Task<(string Json, BookingProposalDto? Proposal)> ExecuteCreateProposalAsync(UserContext caller, string argumentsJson, CancellationToken cancellationToken)
    {
        try
        {
            using var doc = JsonDocument.Parse(argumentsJson);
            var root = doc.RootElement;

            int hotelId = root.TryGetProperty("hotel_id", out var hidProp) && hidProp.TryGetInt32(out var hid) ? hid : 0;
            int roomId = root.TryGetProperty("room_id", out var ridProp) && ridProp.TryGetInt32(out var rid) ? rid : 0;
            int packageId = root.TryGetProperty("package_id", out var pidProp) && pidProp.TryGetInt32(out var pid) ? pid : 0;

            if (!root.TryGetProperty("check_in", out var ciProp) || !DateTime.TryParse(ciProp.GetString(), Inv, DateTimeStyles.AssumeUniversal, out var checkIn))
                return (JsonSerializer.Serialize(new { error = "Valid check_in date is required" }), null);

            DateTime? checkOut = null;
            if (root.TryGetProperty("check_out", out var coProp) && DateTime.TryParse(coProp.GetString(), Inv, DateTimeStyles.AssumeUniversal, out var co))
                checkOut = co;

            var guests = root.TryGetProperty("guests", out var gProp) && gProp.TryGetInt32(out var g) ? g : 1;

            if (hotelId > 0 || roomId > 0)
            {
                if (roomId <= 0 && hotelId > 0)
                {
                    var hotel = await _hotelService.GetAsync(caller, hotelId, cancellationToken);
                    var room = hotel.Rooms.FirstOrDefault(r => r.IsAvailable && r.Capacity >= guests) ?? hotel.Rooms.FirstOrDefault();
                    if (room == null)
                        return (JsonSerializer.Serialize(new { error = "No rooms available at this property." }), null);
                    roomId = room.Id;
                }

                if (!checkOut.HasValue) checkOut = checkIn.AddDays(1);

                var quote = await _bookingService.CheckAvailabilityAsync(caller, new AvailabilityQuery
                {
                    RoomId = roomId,
                    CheckIn = checkIn,
                    CheckOut = checkOut.Value,
                    Guests = guests
                }, cancellationToken);

                var expiresAt = _clock.GetUtcNow().UtcDateTime.AddMinutes(Math.Max(5, _aiOptions.BookingProposalMinutes));
                var token = GenerateProposalToken(roomId, null, quote.CheckIn, quote.CheckOut, quote.Guests, quote.TotalPrice, expiresAt);

                var proposal = new BookingProposalDto
                {
                    Kind = "hotel",
                    RoomId = roomId,
                    HotelName = quote.HotelName,
                    RoomName = quote.RoomName,
                    CheckIn = quote.CheckIn,
                    CheckOut = quote.CheckOut,
                    Nights = quote.Nights,
                    Guests = quote.Guests,
                    TotalPrice = quote.TotalPrice,
                    ProposalToken = token,
                    ExpiresAt = expiresAt
                };

                return (JsonSerializer.Serialize(new
                {
                    success = true,
                    hotel_name = quote.HotelName,
                    room_name = quote.RoomName,
                    total_price = quote.TotalPrice,
                    nights = quote.Nights,
                    check_in = quote.CheckIn.ToString("yyyy-MM-dd"),
                    check_out = quote.CheckOut.ToString("yyyy-MM-dd")
                }, JsonOptions), proposal);
            }

            return (JsonSerializer.Serialize(new { error = "hotel_id or room_id is required" }), null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Tool create_proposal execution error");
            return (JsonSerializer.Serialize(new { error = ex.Message }), null);
        }
    }

    private static string CleanPlainText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        return text.Trim();
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
