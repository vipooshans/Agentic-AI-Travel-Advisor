using System.Text.Json;
using Microsoft.Extensions.Options;
using TravelAdvisor.Core.Common;
using TravelAdvisor.Core.DTOs.AI;
using TravelAdvisor.Core.DTOs.Bookings;
using TravelAdvisor.Core.DTOs.Users;
using TravelAdvisor.Core.Enums;
using TravelAdvisor.Core.Interfaces;
using TravelAdvisor.Core.Interfaces.Services;
using TravelAdvisor.Infrastructure.AI.Agents;
using TravelAdvisor.Infrastructure.AI.Planning;
using TravelAdvisor.Infrastructure.AI.Safety;
using TravelAdvisor.Infrastructure.AI.Schema;

namespace TravelAdvisor.Infrastructure.AI.Tools;

public static class ToolNames
{
    public const string SearchDestinations = "searchDestinations";
    public const string SearchHotels = "searchHotels";
    public const string SearchTravelPackages = "searchTravelPackages";
    public const string SearchActivities = "searchActivities";
    public const string SearchTransportation = "searchTransportation";
    public const string CheckAvailability = "checkAvailability";
    public const string GetTravelPreferences = "getTravelPreferences";
    public const string GenerateItinerary = "generateItinerary";
    public const string CreateBooking = "createBooking";
}

internal static class ToolArgs
{
    public static T Read<T>(JsonElement arguments) =>
        arguments.Deserialize<T>(TravelPlanSchema.JsonOptions)
        ?? throw new BusinessRuleException("Arguments are required.");

    public static DateTime ToUtc(DateOnly date) => DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
}

public sealed class SearchDestinationsTool(ICatalogTools catalog) : IAgentTool
{
    public string Name => ToolNames.SearchDestinations;
    public string Description => "Search catalog destinations by name or country. Omit query to list all destinations.";
    public IReadOnlyCollection<string> AllowedRoles => [];
    public string ParametersSchema => """
        {"type":"object","additionalProperties":false,
         "properties":{"query":{"type":"string","maxLength":100,"description":"Destination name or country"}}}
        """;

    private sealed record Args(string? Query);

    public async Task<object?> ExecuteAsync(JsonElement arguments, AgentToolContext context, CancellationToken cancellationToken)
    {
        var args = ToolArgs.Read<Args>(arguments);
        var results = await catalog.SearchDestinationsAsync(args.Query, cancellationToken);
        foreach (var d in results)
            d.Description = PromptInjectionGuard.SanitizeUntrusted(d.Description);
        return results;
    }
}

public sealed class SearchHotelsTool(ICatalogTools catalog) : IAgentTool
{
    public string Name => ToolNames.SearchHotels;
    public string Description => "Search approved hotel rooms in a city. Prices are per room per night in LKR.";
    public IReadOnlyCollection<string> AllowedRoles => [];
    public string ParametersSchema => """
        {"type":"object","additionalProperties":false,"required":["city"],
         "properties":{
           "city":{"type":"string","minLength":1,"maxLength":100},
           "guests":{"type":"integer","minimum":1,"maximum":20,"description":"Minimum room capacity"},
           "maxPricePerNight":{"type":"number","exclusiveMinimum":0}}}
        """;

    private sealed record Args(string City, int? Guests, decimal? MaxPricePerNight);

    public async Task<object?> ExecuteAsync(JsonElement arguments, AgentToolContext context, CancellationToken cancellationToken)
    {
        var args = ToolArgs.Read<Args>(arguments);
        var results = await catalog.SearchHotelsAsync(args.City, args.Guests ?? 1, args.MaxPricePerNight, cancellationToken);
        foreach (var h in results)
            h.Description = PromptInjectionGuard.SanitizeUntrusted(h.Description);
        return results.Take(20).ToList();
    }
}

public sealed class SearchTravelPackagesTool(ICatalogTools catalog) : IAgentTool
{
    public string Name => ToolNames.SearchTravelPackages;
    public string Description =>
        "Search approved travel packages. pricePerPerson includes every activity; multiply by travelers for the total.";
    public IReadOnlyCollection<string> AllowedRoles => [];
    public string ParametersSchema => """
        {"type":"object","additionalProperties":false,
         "properties":{
           "destination":{"type":"string","maxLength":100},
           "destinationId":{"type":"integer","minimum":1},
           "maxPricePerPerson":{"type":"number","exclusiveMinimum":0},
           "maxDurationDays":{"type":"integer","minimum":1,"maximum":30}}}
        """;

    private sealed record Args(string? Destination, int? DestinationId, decimal? MaxPricePerPerson, int? MaxDurationDays);

    public async Task<object?> ExecuteAsync(JsonElement arguments, AgentToolContext context, CancellationToken cancellationToken)
    {
        var args = ToolArgs.Read<Args>(arguments);
        var results = await catalog.SearchPackagesAsync(args.DestinationId, args.Destination, null, args.MaxDurationDays, cancellationToken);
        if (args.MaxPricePerPerson.HasValue)
            results = results.Where(p => p.PricePerPerson <= args.MaxPricePerPerson.Value).ToList();
        foreach (var p in results)
        {
            p.Description = PromptInjectionGuard.SanitizeUntrusted(p.Description);
            foreach (var a in p.Activities)
                a.Description = PromptInjectionGuard.SanitizeUntrusted(a.Description, 120);
        }

        return results.Take(10).ToList();
    }
}

public sealed class SearchActivitiesTool(ICatalogTools catalog) : IAgentTool
{
    public string Name => ToolNames.SearchActivities;
    public string Description => "Search activities offered in approved packages, optionally by destination, category (nature, hiking, culture, beach, adventure, food, sightseeing) or max price per person.";
    public IReadOnlyCollection<string> AllowedRoles => [];
    public string ParametersSchema => """
        {"type":"object","additionalProperties":false,
         "properties":{
           "destination":{"type":"string","maxLength":100},
           "destinationId":{"type":"integer","minimum":1},
           "category":{"type":"string","maxLength":50},
           "maxPrice":{"type":"number","minimum":0}}}
        """;

    private sealed record Args(string? Destination, int? DestinationId, string? Category, decimal? MaxPrice);

    public async Task<object?> ExecuteAsync(JsonElement arguments, AgentToolContext context, CancellationToken cancellationToken)
    {
        var args = ToolArgs.Read<Args>(arguments);
        var results = await catalog.SearchActivitiesAsync(args.DestinationId, args.Destination, args.Category, args.MaxPrice, cancellationToken);
        foreach (var a in results)
            a.Description = PromptInjectionGuard.SanitizeUntrusted(a.Description, 120);
        return results;
    }
}

public sealed class SearchTransportationTool(ICatalogTools catalog) : IAgentTool
{
    public string Name => ToolNames.SearchTransportation;
    public string Description => "Search active transport options to a destination. Prices are per person per one-way trip in LKR.";
    public IReadOnlyCollection<string> AllowedRoles => [];
    public string ParametersSchema => """
        {"type":"object","additionalProperties":false,
         "properties":{
           "destination":{"type":"string","maxLength":100},
           "destinationId":{"type":"integer","minimum":1},
           "from":{"type":"string","maxLength":100},
           "mode":{"enum":["Bus","Train","Car","Van","TukTuk","Flight","Ferry"]},
           "maxPricePerPerson":{"type":"number","minimum":0}}}
        """;

    private sealed record Args(string? Destination, int? DestinationId, string? From, string? Mode, decimal? MaxPricePerPerson);

    public async Task<object?> ExecuteAsync(JsonElement arguments, AgentToolContext context, CancellationToken cancellationToken)
    {
        var args = ToolArgs.Read<Args>(arguments);
        TransportMode? mode = Enum.TryParse<TransportMode>(args.Mode, true, out var parsed) ? parsed : null;
        var results = await catalog.SearchTransportationAsync(args.DestinationId, args.Destination, args.From, mode, args.MaxPricePerPerson, cancellationToken);
        foreach (var t in results)
            t.Description = PromptInjectionGuard.SanitizeUntrusted(t.Description, 120);
        return results;
    }
}

public sealed class CheckAvailabilityTool(IBookingService bookings) : IAgentTool
{
    public string Name => ToolNames.CheckAvailability;
    public string Description =>
        "Check live availability and the exact price for a room (roomId, checkIn, checkOut) or a package (travelPackageId, checkIn) for a number of guests. Read-only.";
    public IReadOnlyCollection<string> AllowedRoles => [];
    public string ParametersSchema => """
        {"type":"object","additionalProperties":false,"required":["checkIn","guests"],
         "properties":{
           "roomId":{"type":"integer","minimum":1},
           "travelPackageId":{"type":"integer","minimum":1},
           "checkIn":{"type":"string","pattern":"^\\d{4}-\\d{2}-\\d{2}$"},
           "checkOut":{"type":"string","pattern":"^\\d{4}-\\d{2}-\\d{2}$"},
           "guests":{"type":"integer","minimum":1,"maximum":50}},
         "oneOf":[{"required":["roomId"],"not":{"required":["travelPackageId"]}},
                  {"required":["travelPackageId"],"not":{"required":["roomId"]}}]}
        """;

    private sealed record Args(int? RoomId, int? TravelPackageId, DateOnly CheckIn, DateOnly? CheckOut, int Guests);

    public async Task<object?> ExecuteAsync(JsonElement arguments, AgentToolContext context, CancellationToken cancellationToken)
    {
        var args = ToolArgs.Read<Args>(arguments);
        return await bookings.CheckAvailabilityAsync(context.Caller, new AvailabilityQuery
        {
            RoomId = args.RoomId,
            TravelPackageId = args.TravelPackageId,
            CheckIn = ToolArgs.ToUtc(args.CheckIn),
            CheckOut = args.CheckOut is { } checkOut ? ToolArgs.ToUtc(checkOut) : null,
            Guests = args.Guests
        }, cancellationToken);
    }
}

public sealed class GetTravelPreferencesTool(IUserService users) : IAgentTool
{
    public string Name => ToolNames.GetTravelPreferences;
    public string Description => "Read the signed-in user's own saved travel preferences (budget range, interests, accommodation and transport preference).";

    public IReadOnlyCollection<string> AllowedRoles => [RoleNames.User];

    /// <summary>No user id parameter: the tool can only ever read the caller's own preferences.</summary>
    public string ParametersSchema => """{"type":"object","additionalProperties":false,"properties":{}}""";

    public async Task<object?> ExecuteAsync(JsonElement arguments, AgentToolContext context, CancellationToken cancellationToken) =>
        await users.GetPreferencesAsync(context.Caller.UserId, cancellationToken);
}

public sealed class GenerateItineraryTool(RecommendationAgent recommendation, ItineraryAgent itinerary, IOptions<AiOptions> options) : IAgentTool
{
    public string Name => ToolNames.GenerateItinerary;
    public string Description =>
        "Build a complete, budget-compliant trip plan from catalog data: searches hotels, packages, activities and transport, " +
        "checks availability and returns a validated structured plan. Budget is the total for the whole group.";
    public IReadOnlyCollection<string> AllowedRoles => [];
    public string ParametersSchema => """
        {"type":"object","additionalProperties":false,"required":["destination","budget"],
         "properties":{
           "destination":{"type":"string","minLength":1,"maxLength":100},
           "budget":{"type":"number","exclusiveMinimum":0},
           "currency":{"enum":["LKR","USD","EUR","GBP"]},
           "durationDays":{"type":"integer","minimum":1,"maximum":60},
           "travelers":{"type":"integer","minimum":1,"maximum":50},
           "startDate":{"type":"string","pattern":"^\\d{4}-\\d{2}-\\d{2}$"},
           "interests":{"type":"string","maxLength":200},
           "accommodationPreference":{"enum":["budget","mid-range","luxury"]},
           "transportPreference":{"enum":["Bus","Train","Car","Van","TukTuk","Flight","Ferry"]},
           "origin":{"type":"string","maxLength":100},
           "assumptions":{"type":"array","maxItems":8,"items":{"type":"string","maxLength":300}},
           "warnings":{"type":"array","maxItems":8,"items":{"type":"string","maxLength":300}}}}
        """;

    private sealed record Args(
        string Destination,
        decimal Budget,
        string? Currency,
        int? DurationDays,
        int? Travelers,
        DateOnly? StartDate,
        string? Interests,
        string? AccommodationPreference,
        string? TransportPreference,
        string? Origin,
        List<string>? Assumptions,
        List<string>? Warnings);

    public async Task<object?> ExecuteAsync(JsonElement arguments, AgentToolContext context, CancellationToken cancellationToken)
    {
        var args = ToolArgs.Read<Args>(arguments);
        var req = new TripRequirements
        {
            Destination = args.Destination.Trim(),
            DurationDays = args.DurationDays,
            Travelers = args.Travelers ?? 0,
            Interests = args.Interests,
            AccommodationPreference = args.AccommodationPreference,
            TransportPreference = args.TransportPreference,
            Origin = args.Origin,
            Assumptions = args.Assumptions?.Select(a => PromptInjectionGuard.SanitizeUntrusted(a, 300)!).ToList() ?? [],
            Warnings = args.Warnings?.Select(w => PromptInjectionGuard.SanitizeUntrusted(w, 300)!).ToList() ?? []
        };

        if (args.StartDate is { } start)
        {
            if (start < context.Today)
                req.Warnings.Add($"The start date {start:yyyy-MM-dd} is in the past, so I used the default start date instead.");
            else
                req.StartDate = ToolArgs.ToUtc(start);
        }

        RequirementParser.ApplyBudget(req, new BudgetMatch(args.Budget, (args.Currency ?? "LKR").ToUpperInvariant()), options.Value.LkrPerUnit);
        RequirementParser.ApplyDefaults(req, context.Today);
        if (req.Budget is null or <= 0)
            throw new BusinessRuleException("A budget in LKR is required to build a plan.");

        var recommended = await recommendation.RecommendAsync(req, context, cancellationToken);
        var outcome = itinerary.Build(req, recommended, context);
        context.LastOutcome = outcome;
        return outcome;
    }
}

public sealed class CreateBookingTool(IBookingService bookings, ICatalogTools catalog, TimeProvider clock, IOptions<AiOptions> options) : IAgentTool
{
    public string Name => ToolNames.CreateBooking;
    public string Description =>
        "Prepare a booking for a room or package. This never books directly: it checks availability and returns a proposal " +
        "the user must explicitly confirm in their next message. The created booking is PENDING until the provider confirms it.";
    public IReadOnlyCollection<string> AllowedRoles => [RoleNames.User];
    public string ParametersSchema => """
        {"type":"object","additionalProperties":false,"required":["checkIn","guests"],
         "properties":{
           "roomId":{"type":"integer","minimum":1},
           "travelPackageId":{"type":"integer","minimum":1},
           "checkIn":{"type":"string","pattern":"^\\d{4}-\\d{2}-\\d{2}$"},
           "checkOut":{"type":"string","pattern":"^\\d{4}-\\d{2}-\\d{2}$"},
           "guests":{"type":"integer","minimum":1,"maximum":50},
           "notes":{"type":"string","maxLength":500}},
         "oneOf":[{"required":["roomId","checkOut"],"not":{"required":["travelPackageId"]}},
                  {"required":["travelPackageId"],"not":{"required":["roomId"]}}]}
        """;

    private sealed record Args(int? RoomId, int? TravelPackageId, DateOnly CheckIn, DateOnly? CheckOut, int Guests, string? Notes);

    public async Task<object?> ExecuteAsync(JsonElement arguments, AgentToolContext context, CancellationToken cancellationToken)
    {
        context.UseAgent(AgentNames.Booking);
        var args = ToolArgs.Read<Args>(arguments);

        if (context.ConfirmedProposal is { } confirmed)
            return await CreateConfirmedAsync(args, confirmed, context, cancellationToken);

        var quote = await bookings.CheckAvailabilityAsync(context.Caller, new AvailabilityQuery
        {
            RoomId = args.RoomId,
            TravelPackageId = args.TravelPackageId,
            CheckIn = ToolArgs.ToUtc(args.CheckIn),
            CheckOut = args.CheckOut is { } co ? ToolArgs.ToUtc(co) : null,
            Guests = args.Guests
        }, cancellationToken);

        if (!quote.Available)
            return new BookingToolResult { Status = "unavailable", Message = quote.Reason ?? "Not available for those dates." };

        string title;
        int? hotelId = null;
        if (args.RoomId is { } roomId)
        {
            var room = await catalog.GetRoomAsync(roomId, cancellationToken)
                       ?? throw new NotFoundException("Room not found.");
            title = $"{room.HotelName} – {room.RoomName}";
            hotelId = room.HotelId;
        }
        else
        {
            var package = await catalog.GetPackageAsync(args.TravelPackageId!.Value, cancellationToken)
                          ?? throw new NotFoundException("Package not found.");
            title = package.Title;
        }

        var proposal = new BookingProposal
        {
            Id = Guid.NewGuid().ToString("N")[..16],
            Kind = args.RoomId.HasValue ? "room" : "package",
            HotelId = hotelId,
            RoomId = args.RoomId,
            TravelPackageId = args.TravelPackageId,
            Title = title,
            CheckIn = DateOnly.FromDateTime(quote.CheckIn),
            CheckOut = DateOnly.FromDateTime(quote.CheckOut),
            Guests = quote.Guests,
            QuotedTotal = quote.TotalPrice,
            ExpiresAt = clock.GetUtcNow().UtcDateTime.AddMinutes(Math.Max(1, options.Value.BookingProposalMinutes))
        };
        context.NewProposal = proposal;

        return new BookingToolResult
        {
            Status = "confirmation_required",
            Message = "Nothing has been booked. Ask the user to reply 'confirm' to submit this booking request.",
            Proposal = proposal
        };
    }

    private async Task<BookingToolResult> CreateConfirmedAsync(Args args, BookingProposal confirmed, AgentToolContext context, CancellationToken cancellationToken)
    {
        var matches = args.RoomId == confirmed.RoomId
                      && args.TravelPackageId == confirmed.TravelPackageId
                      && args.CheckIn == confirmed.CheckIn
                      && args.Guests == confirmed.Guests
                      && (confirmed.Kind != "room" || args.CheckOut == confirmed.CheckOut);
        if (!matches)
            throw new BusinessRuleException("The booking details do not match the proposal the user confirmed.");

        try
        {
            var booking = await bookings.CreateAsync(context.Caller, new CreateBookingRequest
            {
                RoomId = confirmed.RoomId,
                TravelPackageId = confirmed.TravelPackageId,
                CheckIn = ToolArgs.ToUtc(confirmed.CheckIn),
                CheckOut = confirmed.Kind == "room" && confirmed.CheckOut is { } checkOut ? ToolArgs.ToUtc(checkOut) : null,
                Guests = confirmed.Guests,
                Notes = string.IsNullOrWhiteSpace(args.Notes) ? "Requested via AI travel assistant" : args.Notes.Trim()
            }, cancellationToken);

            context.CreatedBooking = booking;
            return new BookingToolResult
            {
                Status = "created",
                Message = $"Backend created booking #{booking.Id} with status {booking.Status}.",
                Booking = booking
            };
        }
        catch (AppException ex)
        {
            context.BookingError = ex.Message;
            throw;
        }
    }
}

public sealed class BookingToolResult
{
    /// <summary>confirmation_required, unavailable or created.</summary>
    public string Status { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public BookingProposal? Proposal { get; init; }
    public BookingDto? Booking { get; init; }
}
