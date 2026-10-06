using System.Globalization;
using TravelAdvisor.Core.DTOs.AI;
using TravelAdvisor.Core.DTOs.Bookings;
using TravelAdvisor.Infrastructure.AI.Planning;
using TravelAdvisor.Infrastructure.AI.Tools;

namespace TravelAdvisor.Infrastructure.AI.Agents;

/// <summary>
/// Gathers candidates through the catalog tools, picks the best budget-compliant combination and
/// verifies the chosen room and package with live availability checks.
/// </summary>
public class RecommendationAgent
{
    private const int MaxAvailabilityAttempts = 6;

    public async Task<RecommendationResult> RecommendAsync(
        TripRequirements requirements,
        AgentToolContext context,
        CancellationToken cancellationToken = default)
    {
        context.UseAgent(AgentNames.Recommendation);
        var registry = context.Registry;

        var (matches, _) = await registry.InvokeAsync<List<CatalogDestinationMatch>>(
            ToolNames.SearchDestinations, new { query = requirements.Destination }, context, cancellationToken);
        var destination = matches?.FirstOrDefault(d => d.Name.Equals(requirements.Destination, StringComparison.OrdinalIgnoreCase))
                          ?? matches?.FirstOrDefault();
        if (destination is null)
        {
            var (all, _) = await registry.InvokeAsync<List<CatalogDestinationMatch>>(
                ToolNames.SearchDestinations, new { }, context, cancellationToken);
            var known = all?.Select(d => d.Name).ToList() ?? [];
            return new RecommendationResult
            {
                Status = ChatStatus.NoMatch,
                KnownDestinations = known,
                Error = $"I couldn't find \"{requirements.Destination}\" in our catalog." +
                        (known.Count > 0 ? $" I can currently plan trips to {string.Join(", ", known)}." : "")
            };
        }

        var input = new PlanningInput(
            requirements.DurationDays ?? RequirementParser.DefaultDurationDays,
            Math.Max(1, requirements.Travelers),
            requirements.Budget ?? 0,
            requirements.AccommodationPreference ?? "mid-range",
            RequirementParser.SplitInterests(requirements.Interests),
            requirements.TransportPreference);

        var result = new RecommendationResult { Destination = destination, Input = input };
        var checkIn = DateOnly.FromDateTime(requirements.StartDate!.Value);

        List<CatalogHotelMatch> rooms = [];
        if (input.Nights > 0)
        {
            var (found, _) = await registry.InvokeAsync<List<CatalogHotelMatch>>(
                ToolNames.SearchHotels, new { city = destination.Name, guests = 1 }, context, cancellationToken);
            rooms = found ?? [];
        }

        var (packages, _) = await registry.InvokeAsync<List<CatalogPackageMatch>>(
            ToolNames.SearchTravelPackages, new { destinationId = destination.Id, maxDurationDays = input.Days }, context, cancellationToken);

        var (activities, _) = await registry.InvokeAsync<List<CatalogActivityMatch>>(
            ToolNames.SearchActivities, new { destinationId = destination.Id }, context, cancellationToken);
        result.DestinationActivities = activities ?? [];

        var transport = await SearchTransportAsync(requirements, destination, context, result.Warnings, cancellationToken);

        var excludedRooms = new HashSet<int>();
        var excludedPackages = new HashSet<int>();

        for (var attempt = 0; attempt < MaxAvailabilityAttempts; attempt++)
        {
            var hotelOptions = BudgetPlanner.BuildHotelOptions(input, rooms, excludedRooms);
            var packageOptions = BudgetPlanner.BuildPackageOptions(input, packages ?? [], excludedPackages);
            var transportOptions = BudgetPlanner.BuildTransportOptions(input, transport, destination.Name);
            var planned = BudgetPlanner.Select(input, hotelOptions, packageOptions, transportOptions);

            if (planned.Selection is null)
            {
                result.Status = planned.CheapestTotal is null ? ChatStatus.NoMatch : ChatStatus.OverBudget;
                result.CheapestTotal = planned.CheapestTotal;
                result.CheapestDescription = planned.CheapestDescription;
                result.Error = planned.FailureReason;
                return result;
            }

            var selection = planned.Selection;
            if (selection.Hotel is { } hotel)
            {
                var quote = await QuoteAsync(new
                {
                    roomId = hotel.Room.RoomId,
                    checkIn = checkIn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    checkOut = checkIn.AddDays(input.Nights).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    guests = Math.Min(input.Travelers, hotel.Room.Capacity)
                }, context, cancellationToken);

                if (quote is not { Available: true })
                {
                    excludedRooms.Add(hotel.Room.RoomId);
                    result.Warnings.Add($"{hotel.Room.HotelName} ({hotel.Room.RoomName}) is not available for your dates" +
                                        (quote?.Reason is { } reason ? $": {reason}" : "."));
                    continue;
                }

                var quotedTotal = quote.TotalPrice * hotel.Rooms;
                if (quotedTotal != hotel.Total)
                {
                    hotel.Total = quotedTotal;
                    if (selection.Total > input.Budget)
                    {
                        excludedRooms.Add(hotel.Room.RoomId);
                        result.Warnings.Add($"{hotel.Room.HotelName} ({hotel.Room.RoomName}) costs more on your dates than its standard rate.");
                        continue;
                    }
                }

                result.RoomQuote = quote;
            }

            if (selection.Package is { } package)
            {
                var quote = await QuoteAsync(new
                {
                    travelPackageId = package.Package.Id,
                    checkIn = checkIn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    guests = input.Travelers
                }, context, cancellationToken);

                if (quote is not { Available: true } || quote.TotalPrice > package.Total + 1)
                {
                    excludedPackages.Add(package.Package.Id);
                    result.Warnings.Add($"The {package.Package.Title} package is not available for your group on that date" +
                                        (quote?.Reason is { } reason ? $": {reason}" : "."));
                    continue;
                }

                result.PackageQuote = quote;
            }

            result.Selection = selection;
            result.Status = ChatStatus.Plan;
            return result;
        }

        result.Status = ChatStatus.NoMatch;
        result.Error = "I couldn't find hotels or packages that are available for your dates within the budget.";
        return result;
    }

    private static async Task<List<CatalogTransportMatch>> SearchTransportAsync(
        TripRequirements requirements,
        CatalogDestinationMatch destination,
        AgentToolContext context,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(requirements.Origin))
        {
            var (fromOrigin, _) = await context.Registry.InvokeAsync<List<CatalogTransportMatch>>(
                ToolNames.SearchTransportation,
                new { destinationId = destination.Id, destination = destination.Name, from = requirements.Origin },
                context, cancellationToken);
            if (fromOrigin is { Count: > 0 })
                return fromOrigin;
            warnings.Add($"No listed transport from {requirements.Origin}; showing other routes to {destination.Name}.");
        }

        var (any, _) = await context.Registry.InvokeAsync<List<CatalogTransportMatch>>(
            ToolNames.SearchTransportation, new { destinationId = destination.Id, destination = destination.Name }, context, cancellationToken);
        return any ?? [];
    }

    private static async Task<AvailabilityQuoteDto?> QuoteAsync(object args, AgentToolContext context, CancellationToken cancellationToken)
    {
        var (quote, result) = await context.Registry.InvokeAsync<AvailabilityQuoteDto>(ToolNames.CheckAvailability, args, context, cancellationToken);
        return result.Success ? quote : new AvailabilityQuoteDto { Available = false, Reason = result.Error };
    }
}

public class RecommendationResult
{
    public string Status { get; set; } = ChatStatus.Plan;
    public string? Error { get; set; }
    public CatalogDestinationMatch? Destination { get; set; }
    public PlanningInput? Input { get; set; }
    public PlanSelection? Selection { get; set; }
    public List<CatalogActivityMatch> DestinationActivities { get; set; } = [];
    public AvailabilityQuoteDto? RoomQuote { get; set; }
    public AvailabilityQuoteDto? PackageQuote { get; set; }
    public decimal? CheapestTotal { get; set; }
    public string? CheapestDescription { get; set; }
    public List<string> KnownDestinations { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}
