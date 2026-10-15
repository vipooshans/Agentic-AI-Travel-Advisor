using System.Globalization;
using System.Security.Cryptography;
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
    private static readonly Regex DateRangeNamed = new(@"\b(?<month>Jan(?:uary)?|Feb(?:ruary)?|Mar(?:ch)?|Apr(?:il)?|May|Jun(?:e)?|Jul(?:y)?|Aug(?:ust)?|Sep(?:tember)?|Oct(?:ober)?|Nov(?:ember)?|Dec(?:ember)?)\s+(?<day>\d{1,2})(?:st|nd|rd|th)?(?:,?\s*(?<year>\d{4}))?\s*(?:to|until|-|through)\s*(?:(?<endMonth>Jan(?:uary)?|Feb(?:ruary)?|Mar(?:ch)?|Apr(?:il)?|May|Jun(?:e)?|Jul(?:y)?|Aug(?:ust)?|Sep(?:tember)?|Oct(?:ober)?|Nov(?:ember)?|Dec(?:ember)?)\s+)?(?<endDay>\d{1,2})(?:st|nd|rd|th)?(?:,?\s*(?<endYear>\d{4}))?\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

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

        // Build conversation transcript context for LLM
        var conversationContext = string.Join(Environment.NewLine, history
            .TakeLast(10)
            .Select(m => $"{m.Role.ToUpperInvariant()}: {m.Content}"));

        BookingAgentPromptResponse response;

        // 2. Fast-path: Check for cancellation intent
        var cancelMatch = CancelRegex.Match(currentPrompt);
        if (cancelMatch.Success && int.TryParse(cancelMatch.Groups[1].Value, out var cancelId))
        {
            response = await HandleCancellationPreviewAsync(caller, cancelId, cancellationToken);
        }
        // 3. Fast-path: Check for insights intent
        else if (InsightsRegex.IsMatch(currentPrompt) && !currentPrompt.Contains("book", StringComparison.OrdinalIgnoreCase))
        {
            var insights = await GetBookingInsightsAsync(caller, cancellationToken);
            response = new BookingAgentPromptResponse
            {
                ActionType = "insights",
                Message = insights.Count > 0
                    ? $"Found {insights.Count} active reservation insight(s) for your account."
                    : "You currently have no active or upcoming reservations.",
                Insights = insights
            };
        }
        // 4. Try LLM Parsing (Gemini / OpenAI) with conversational flow
        else if (_llmClient.IsConfigured)
        {
            try
            {
                var llmParsed = await ParsePromptWithLlmAsync(conversationContext, cancellationToken);
                if (llmParsed != null)
                {
                    response = await ExecuteParsedIntentAsync(caller, llmParsed, cancellationToken);
                }
                else
                {
                    var fallbackIntent = await FallbackParseIntentAsync(currentPrompt, history, cancellationToken);
                    response = await ExecuteParsedIntentAsync(caller, fallbackIntent, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "LLM parsing failed for booking prompt; falling back to deterministic parser.");
                var fallbackIntent = await FallbackParseIntentAsync(currentPrompt, history, cancellationToken);
                response = await ExecuteParsedIntentAsync(caller, fallbackIntent, cancellationToken);
            }
        }
        else
        {
            // 5. Fallback Parser
            var fallbackIntent = await FallbackParseIntentAsync(currentPrompt, history, cancellationToken);
            response = await ExecuteParsedIntentAsync(caller, fallbackIntent, cancellationToken);
        }

        // Add assistant response to history
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
            conv = await _context.AIConversations
                .OrderByDescending(c => c.UpdatedAt)
                .FirstOrDefaultAsync(c => c.UserId == userId && c.Title == "Booking Concierge", cancellationToken);
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
                ? $"Are you sure you want to cancel your reservation #{booking.Id} for **{listingName}**? Free cancellation deadline is {booking.CheckIn.AddHours(-cutoffHours):MMM dd, yyyy HH:mm} UTC."
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

    private async Task<ParsedIntent?> ParsePromptWithLlmAsync(string conversationContext, CancellationToken cancellationToken)
    {
        var today = _clock.GetUtcNow().UtcDateTime.ToString("yyyy-MM-dd", Inv);
        var systemPrompt = $$"""
            You are the professional AI Booking Concierge for Travel Advisor Sri Lanka, interacting with a traveler.
            Today's date is {{today}}.
            Your tone is warm, polite, and helpful like ChatGPT.

            Analyze the recent conversation and determine the next action:
            1. If the traveler wants to book a hotel or tour package, but has NOT yet provided all necessary details (destination place, check-in date, check-out date, or number of guests):
               - Set "intent": "conversation"
               - Set "reply": Warmly ask the traveler for the missing details naturally (e.g., "I'd love to help you book a stay in Ella! What dates are you thinking of, and how many guests will be staying?").
            2. If you now have the destination, check-in date, check-out date, and guest count (from the current message or previous messages in the context):
               - Set "intent": "book_hotel" or "book_package"
               - Fill "hotelQuery", "roomQuery", "packageQuery", "checkIn" (YYYY-MM-DD), "checkOut" (YYYY-MM-DD), "guests" (number).
               - Set "reply": A friendly sentence stating that you found a great match and prepared their reservation proposal.
            3. If the user asks for a cheaper option or to decrease budget:
               - Set "intent": "book_hotel" (keeping destination and dates)
               - Set "wantsCheaper": true
               - Set "reply": "Certainly! Searching for more affordable stays for your dates..."
            4. If the user asks to upgrade or for luxury:
               - Set "intent": "book_hotel"
               - Set "wantsUpgrade": true
               - Set "reply": "Looking for luxury suites and premium room options..."
            5. If the user asks to change the place/city:
               - Set "intent": "book_hotel"
               - Set "hotelQuery" to the new city, keeping dates and guests.
               - Set "reply": "Let me check available stays in [city] for your dates..."
            6. If the user asks to cancel a booking (e.g. "cancel booking 4"):
               - Set "intent": "cancel_booking"
               - Set "bookingId": number
               - Set "reply": "Checking cancellation details for booking #[id]..."
            7. For general questions, greetings, or help:
               - Set "intent": "conversation"
               - Set "reply": Respond politely and naturally like ChatGPT.

            Return ONLY valid JSON matching this schema:
            {
              "intent": "conversation" | "book_hotel" | "book_package" | "cancel_booking" | "insights" | "unknown",
              "bookingId": number or null,
              "hotelQuery": "hotel or city name" or null,
              "roomQuery": "room type" or null,
              "packageQuery": "package title" or null,
              "checkIn": "YYYY-MM-DD" or null,
              "checkOut": "YYYY-MM-DD" or null,
              "guests": number or null,
              "wantsCheaper": boolean,
              "wantsUpgrade": boolean,
              "maxBudget": number or null,
              "reply": "conversational reply"
            }
            """;

        var response = await _llmClient.CompleteAsync(systemPrompt, conversationContext, jsonMode: true, cancellationToken);
        if (string.IsNullOrWhiteSpace(response)) return null;

        try
        {
            using var doc = JsonDocument.Parse(response);
            var root = doc.RootElement;
            var intent = root.TryGetProperty("intent", out var iProp) ? iProp.GetString() : "conversation";

            return new ParsedIntent
            {
                Intent = intent ?? "conversation",
                BookingId = root.TryGetProperty("bookingId", out var bProp) && bProp.ValueKind == JsonValueKind.Number ? bProp.GetInt32() : null,
                HotelQuery = root.TryGetProperty("hotelQuery", out var hProp) ? hProp.GetString() : null,
                RoomQuery = root.TryGetProperty("roomQuery", out var rProp) ? rProp.GetString() : null,
                PackageQuery = root.TryGetProperty("packageQuery", out var pProp) ? pProp.GetString() : null,
                CheckIn = ParseDateOrNull(root.TryGetProperty("checkIn", out var cInProp) ? cInProp.GetString() : null),
                CheckOut = ParseDateOrNull(root.TryGetProperty("checkOut", out var cOutProp) ? cOutProp.GetString() : null),
                Guests = root.TryGetProperty("guests", out var gProp) && gProp.ValueKind == JsonValueKind.Number ? Math.Max(1, gProp.GetInt32()) : 2,
                WantsCheaper = root.TryGetProperty("wantsCheaper", out var wc) && wc.GetBoolean(),
                WantsUpgrade = root.TryGetProperty("wantsUpgrade", out var wu) && wu.GetBoolean(),
                MaxBudget = root.TryGetProperty("maxBudget", out var mb) && mb.ValueKind == JsonValueKind.Number ? mb.GetDecimal() : null,
                Reply = root.TryGetProperty("reply", out var repProp) ? repProp.GetString() : null
            };
        }
        catch
        {
            return null;
        }
    }

    private async Task<ParsedIntent> FallbackParseIntentAsync(string currentPrompt, List<BookingAgentMessageDto> history, CancellationToken cancellationToken)
    {
        var allText = string.Join(" ", history.Select(h => h.Content).Append(currentPrompt));
        var location = ExtractLocationOrName(allText);

        DateTime? checkIn = null;
        DateTime? checkOut = null;

        var isoMatch = DateRangeIso.Match(allText);
        if (isoMatch.Success)
        {
            if (DateTime.TryParse(isoMatch.Groups[1].Value, Inv, DateTimeStyles.AssumeUniversal, out var inDt))
                checkIn = inDt;
            if (DateTime.TryParse(isoMatch.Groups[2].Value, Inv, DateTimeStyles.AssumeUniversal, out var outDt))
                checkOut = outDt;
        }
        else
        {
            var namedMatch = DateRangeNamed.Match(allText);
            if (namedMatch.Success)
            {
                var currentYear = _clock.GetUtcNow().UtcDateTime.Year;
                var startMonth = namedMatch.Groups["month"].Value;
                var startDay = int.Parse(namedMatch.Groups["day"].Value, Inv);
                var startYear = namedMatch.Groups["year"].Success ? int.Parse(namedMatch.Groups["year"].Value, Inv) : currentYear;

                var endMonth = namedMatch.Groups["endMonth"].Success ? namedMatch.Groups["endMonth"].Value : startMonth;
                var endDay = int.Parse(namedMatch.Groups["endDay"].Value, Inv);
                var endYear = namedMatch.Groups["endYear"].Success ? int.Parse(namedMatch.Groups["endYear"].Value, Inv) : startYear;

                if (DateTime.TryParse($"{startMonth} {startDay}, {startYear}", Inv, DateTimeStyles.AssumeUniversal, out var inNamed))
                    checkIn = inNamed;
                if (DateTime.TryParse($"{endMonth} {endDay}, {endYear}", Inv, DateTimeStyles.AssumeUniversal, out var outNamed))
                    checkOut = outNamed;
            }
        }

        var guests = 2;
        var guestMatch = GuestsRegex.Match(allText);
        if (guestMatch.Success && int.TryParse(guestMatch.Groups[1].Value, out var g))
            guests = Math.Clamp(g, 1, 10);

        var isPackage = allText.Contains("package", StringComparison.OrdinalIgnoreCase) || allText.Contains("tour", StringComparison.OrdinalIgnoreCase);
        var wantsCheaper = Regex.IsMatch(currentPrompt, @"\b(cheaper|lower budget|decrease budget|less expensive|economy|save)\b", RegexOptions.IgnoreCase);
        var wantsUpgrade = Regex.IsMatch(currentPrompt, @"\b(upgrade|higher budget|increase budget|luxury|suite|premium)\b", RegexOptions.IgnoreCase);

        // If dates or destination are missing, ask conversationally like ChatGPT!
        if (string.IsNullOrWhiteSpace(location))
        {
            return new ParsedIntent
            {
                Intent = "conversation",
                Reply = "Hello! I would be delighted to help you plan your journey in Sri Lanka. Where would you like to visit (e.g. Ella, Kandy, Galle, Sigiriya, Mirissa)?"
            };
        }

        if (!checkIn.HasValue)
        {
            return new ParsedIntent
            {
                Intent = "conversation",
                HotelQuery = location,
                Reply = $"I would love to help you find a stay in **{CultureInfo.CurrentCulture.TextInfo.ToTitleCase(location)}**! What dates are you planning for your visit, and how many guests will be traveling?"
            };
        }

        return new ParsedIntent
        {
            Intent = isPackage ? "book_package" : "book_hotel",
            HotelQuery = location,
            PackageQuery = location,
            CheckIn = checkIn,
            CheckOut = checkOut ?? checkIn.Value.AddDays(2),
            Guests = guests,
            WantsCheaper = wantsCheaper,
            WantsUpgrade = wantsUpgrade
        };
    }

    private async Task<BookingAgentPromptResponse> ExecuteParsedIntentAsync(UserContext caller, ParsedIntent intent, CancellationToken cancellationToken)
    {
        if (intent.Intent == "conversation")
        {
            return new BookingAgentPromptResponse
            {
                ActionType = "chat",
                Message = intent.Reply ?? "How can I help you plan your trip?"
            };
        }

        if (intent.Intent == "cancel_booking" && intent.BookingId.HasValue)
        {
            return await HandleCancellationPreviewAsync(caller, intent.BookingId.Value, cancellationToken);
        }

        if (intent.Intent == "insights")
        {
            var insights = await GetBookingInsightsAsync(caller, cancellationToken);
            return new BookingAgentPromptResponse
            {
                ActionType = "insights",
                Message = $"Found {insights.Count} active reservation insight(s).",
                Insights = insights
            };
        }

        if (intent.Intent == "book_package")
        {
            return await ProposePackageBookingAsync(caller, intent, cancellationToken);
        }

        return await ProposeHotelBookingAsync(caller, intent, cancellationToken);
    }

    private async Task<BookingAgentPromptResponse> ProposeHotelBookingAsync(UserContext caller, ParsedIntent intent, CancellationToken cancellationToken)
    {
        var hotels = await _hotelService.SearchAsync(caller, new HotelSearchQuery
        {
            Q = intent.HotelQuery,
            Guests = intent.Guests
        }, cancellationToken);

        if (hotels.Count == 0)
        {
            hotels = await _hotelService.SearchAsync(caller, new HotelSearchQuery(), cancellationToken);
        }

        if (hotels.Count == 0)
        {
            return new BookingAgentPromptResponse
            {
                ActionType = "chat",
                Message = "No approved hotels are currently available in the catalog matching that destination."
            };
        }

        if (intent.WantsCheaper || intent.MaxBudget.HasValue)
            hotels = hotels.OrderBy(h => h.MinPricePerNight ?? decimal.MaxValue).ToList();
        else if (intent.WantsUpgrade)
            hotels = hotels.OrderByDescending(h => h.MinPricePerNight ?? 0).ToList();

        var selectedHotel = hotels.FirstOrDefault();
        var hotelDetail = await _hotelService.GetAsync(null, selectedHotel!.Id, cancellationToken);

        if (hotelDetail.Rooms.Count == 0)
        {
            return new BookingAgentPromptResponse
            {
                ActionType = "chat",
                Message = $"We found **{hotelDetail.Name}**, but there are no rooms listed for booking right now."
            };
        }

        var candidateRooms = hotelDetail.Rooms.Where(r => r.IsAvailable && r.Capacity >= intent.Guests).ToList();
        if (candidateRooms.Count == 0) candidateRooms = hotelDetail.Rooms.Where(r => r.IsAvailable).ToList();
        if (candidateRooms.Count == 0) candidateRooms = hotelDetail.Rooms;

        RoomDto room;
        if (intent.WantsCheaper)
            room = candidateRooms.OrderBy(r => r.PricePerNight).First();
        else if (intent.WantsUpgrade)
            room = candidateRooms.OrderByDescending(r => r.PricePerNight).First();
        else
            room = candidateRooms.FirstOrDefault(r => r.Capacity >= intent.Guests) ?? candidateRooms.First();

        var checkIn = intent.CheckIn ?? _clock.GetUtcNow().UtcDateTime.Date.AddDays(7);
        var checkOut = intent.CheckOut ?? checkIn.AddDays(2);

        var quote = await _bookingService.CheckAvailabilityAsync(caller, new AvailabilityQuery
        {
            RoomId = room.Id,
            CheckIn = checkIn,
            CheckOut = checkOut,
            Guests = intent.Guests
        }, cancellationToken);

        if (!quote.Available)
        {
            return new BookingAgentPromptResponse
            {
                ActionType = "chat",
                Message = $"**{hotelDetail.Name} - {room.Name}** is not available for those dates: {quote.Reason}. Would you like to check different dates?"
            };
        }

        var expiresAt = _clock.GetUtcNow().UtcDateTime.AddMinutes(Math.Max(5, _aiOptions.BookingProposalMinutes));
        var token = GenerateProposalToken(room.Id, null, quote.CheckIn, quote.CheckOut, quote.Guests, quote.TotalPrice, expiresAt);

        var proposal = new BookingProposalDto
        {
            Kind = "hotel",
            HotelId = hotelDetail.Id,
            HotelName = hotelDetail.Name,
            RoomId = room.Id,
            RoomName = room.Name,
            CheckIn = quote.CheckIn,
            CheckOut = quote.CheckOut,
            Nights = quote.Nights,
            Guests = quote.Guests,
            TotalPrice = quote.TotalPrice,
            ProposalToken = token,
            ExpiresAt = expiresAt
        };

        var replyMessage = intent.Reply ?? $"I have prepared a reservation proposal for **{room.Name}** at **{hotelDetail.Name}** from **{quote.CheckIn:MMM dd, yyyy}** to **{quote.CheckOut:MMM dd, yyyy}** ({quote.Nights} nights, {quote.Guests} guest(s)) for **LKR {quote.TotalPrice:N0}**.";

        return new BookingAgentPromptResponse
        {
            ActionType = "proposal",
            Message = replyMessage,
            Proposal = proposal
        };
    }

    private async Task<BookingAgentPromptResponse> ProposePackageBookingAsync(UserContext caller, ParsedIntent intent, CancellationToken cancellationToken)
    {
        var packages = await _packageService.SearchAsync(caller, new PackageSearchQuery
        {
            Q = intent.PackageQuery
        }, cancellationToken);

        if (intent.WantsCheaper || intent.MaxBudget.HasValue)
            packages = packages.OrderBy(p => p.Price).ToList();
        else if (intent.WantsUpgrade)
            packages = packages.OrderByDescending(p => p.Price).ToList();

        var pkg = packages.FirstOrDefault(p => p.MaxTravelers >= intent.Guests);
        if (pkg is null)
        {
            packages = await _packageService.SearchAsync(caller, new PackageSearchQuery(), cancellationToken);
            pkg = packages.FirstOrDefault(p => p.MaxTravelers >= intent.Guests);
        }

        if (pkg is null)
        {
            return new BookingAgentPromptResponse
            {
                ActionType = "chat",
                Message = packages.Count == 0
                    ? "No approved tour packages were found matching your criteria."
                    : $"No approved tour package can accommodate {intent.Guests} traveler(s)."
            };
        }

        var checkIn = intent.CheckIn ?? _clock.GetUtcNow().UtcDateTime.Date.AddDays(7);

        var quote = await _bookingService.CheckAvailabilityAsync(caller, new AvailabilityQuery
        {
            TravelPackageId = pkg.Id,
            CheckIn = checkIn,
            Guests = intent.Guests
        }, cancellationToken);

        if (!quote.Available)
        {
            return new BookingAgentPromptResponse
            {
                ActionType = "chat",
                Message = $"Tour package **{pkg.Title}** is not available on {checkIn:MMM dd, yyyy}: {quote.Reason}."
            };
        }

        var expiresAt = _clock.GetUtcNow().UtcDateTime.AddMinutes(Math.Max(5, _aiOptions.BookingProposalMinutes));
        var token = GenerateProposalToken(null, pkg.Id, quote.CheckIn, quote.CheckOut, quote.Guests, quote.TotalPrice, expiresAt);

        var proposal = new BookingProposalDto
        {
            Kind = "package",
            TravelPackageId = pkg.Id,
            PackageTitle = pkg.Title,
            CheckIn = quote.CheckIn,
            CheckOut = quote.CheckOut,
            Nights = quote.Nights,
            Guests = quote.Guests,
            TotalPrice = quote.TotalPrice,
            ProposalToken = token,
            ExpiresAt = expiresAt
        };

        var replyMessage = intent.Reply ?? $"I have prepared a reservation proposal for **{pkg.Title}** starting **{quote.CheckIn:MMM dd, yyyy}** ({pkg.DurationDays} days, {quote.Guests} traveler(s)) for **LKR {quote.TotalPrice:N0}**.";

        return new BookingAgentPromptResponse
        {
            ActionType = "proposal",
            Message = replyMessage,
            Proposal = proposal
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

    private static DateTime? ParseDateOrNull(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        return DateTime.TryParse(raw, Inv, DateTimeStyles.AssumeUniversal, out var dt) ? dt : null;
    }

    private static string? ExtractLocationOrName(string prompt)
    {
        var words = new[] { "ella", "galle", "kandy", "colombo", "sigiriya", "mirissa", "nuwara eliya", "bentota", "yala" };
        foreach (var w in words)
        {
            if (prompt.Contains(w, StringComparison.OrdinalIgnoreCase))
                return w;
        }
        return null;
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

    private sealed class ParsedIntent
    {
        public string Intent { get; set; } = "conversation";
        public int? BookingId { get; set; }
        public string? HotelQuery { get; set; }
        public string? RoomQuery { get; set; }
        public string? PackageQuery { get; set; }
        public DateTime? CheckIn { get; set; }
        public DateTime? CheckOut { get; set; }
        public int Guests { get; set; } = 2;
        public decimal? MaxBudget { get; set; }
        public bool WantsCheaper { get; set; }
        public bool WantsUpgrade { get; set; }
        public string? Reply { get; set; }
    }
}
