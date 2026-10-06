using System.Globalization;
using Microsoft.Extensions.Options;
using TravelAdvisor.Core.DTOs.AI;
using TravelAdvisor.Core.DTOs.Users;
using TravelAdvisor.Core.Enums;
using TravelAdvisor.Infrastructure.AI.Planning;
using TravelAdvisor.Infrastructure.AI.Tools;

namespace TravelAdvisor.Infrastructure.AI.Agents;

/// <summary>Understands the request: extracts requirements and fills gaps from the user's saved preferences.</summary>
public class TravelPlanningAgent
{
    private readonly AiOptions _options;

    public TravelPlanningAgent(IOptions<AiOptions> options)
    {
        _options = options.Value;
    }

    public async Task<(TripRequirements Requirements, IReadOnlyList<string> KnownDestinations)> ExtractAsync(
        IReadOnlyList<ChatMessageDto> history,
        AgentToolContext context,
        CancellationToken cancellationToken = default)
    {
        context.UseAgent(AgentNames.Planning);

        var (destinations, _) = await context.Registry.InvokeAsync<List<CatalogDestinationMatch>>(
            ToolNames.SearchDestinations, new { }, context, cancellationToken);
        var names = destinations?.Select(d => d.Name).ToList() ?? [];

        var req = RequirementParser.Parse(history, names, context.Today, _options.LkrPerUnit);

        if (context.Caller.Role == RoleNames.User)
        {
            var (prefs, _) = await context.Registry.InvokeAsync<TravelPreferencesDto>(
                ToolNames.GetTravelPreferences, new { }, context, cancellationToken);
            if (prefs is not null)
                MergePreferences(req, prefs);
        }

        req.MissingFields.Clear();
        if (string.IsNullOrWhiteSpace(req.Destination))
            req.MissingFields.Add("destination");
        if (req.Budget is null or <= 0)
            req.MissingFields.Add("budget");

        return (req, names);
    }

    public static void MergePreferences(TripRequirements req, TravelPreferencesDto prefs)
    {
        if (string.IsNullOrWhiteSpace(req.Interests) && !string.IsNullOrWhiteSpace(prefs.Interests))
        {
            req.Interests = prefs.Interests;
            req.Assumptions.Add($"Used your saved interests: {prefs.Interests}.");
        }

        if (string.IsNullOrWhiteSpace(req.AccommodationPreference) && !string.IsNullOrWhiteSpace(prefs.AccommodationPreference))
        {
            req.AccommodationPreference = prefs.AccommodationPreference;
            req.Assumptions.Add($"Used your saved {prefs.AccommodationPreference} accommodation preference.");
        }

        if (string.IsNullOrWhiteSpace(req.TransportPreference) && !string.IsNullOrWhiteSpace(prefs.TransportPreference))
        {
            req.TransportPreference = prefs.TransportPreference;
            req.Assumptions.Add($"Used your saved transport preference ({prefs.TransportPreference}).");
        }

        if (req.Budget is null or <= 0 && req.OriginalCurrency is null && prefs.BudgetMax is > 0)
        {
            req.Budget = prefs.BudgetMax;
            req.Assumptions.Add($"Used your saved maximum budget of Rs. {prefs.BudgetMax.Value.ToString("N0", CultureInfo.InvariantCulture)}.");
        }
    }

    public static string BuildClarifyingMessage(TripRequirements req, IReadOnlyList<string> knownDestinations)
    {
        var options = knownDestinations.Count > 0
            ? string.Join(", ", knownDestinations)
            : "Ella, Kandy or Galle";
        var warning = req.Warnings.Count > 0 ? " " + string.Join(" ", req.Warnings) : "";

        if (req.MissingFields.Contains("destination") && req.MissingFields.Contains("budget"))
            return $"I can plan that. Which destination would you like (I can currently plan trips to {options}), and what is your total budget (for example Rs. 50,000)?{warning}";
        if (req.MissingFields.Contains("destination"))
            return $"Got your budget. Which destination should I plan for? I can currently plan trips to {options}.{warning}";
        if (req.MissingFields.Contains("budget"))
            return $"Great choice. What is your total budget for the trip (for example Rs. 50,000)?{warning}";
        return "Tell me the destination and budget and I will put a plan together.";
    }
}
