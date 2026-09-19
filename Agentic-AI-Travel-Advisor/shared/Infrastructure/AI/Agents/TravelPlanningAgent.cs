using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using TravelAdvisor.Core.DTOs.AI;
using TravelAdvisor.Core.Interfaces;

namespace TravelAdvisor.Infrastructure.AI.Agents;

public class TravelPlanningAgent
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ILlmClient _llm;

    public TravelPlanningAgent(ILlmClient llm)
    {
        _llm = llm;
    }

    public async Task<TripRequirements> ExtractAsync(
        IReadOnlyList<ChatMessageDto> history,
        IReadOnlyList<string> knownDestinations,
        CancellationToken cancellationToken = default)
    {
        TripRequirements? extracted = null;

        if (_llm.IsConfigured)
        {
            try
            {
                extracted = await ExtractWithLlmAsync(history, knownDestinations, cancellationToken);
            }
            catch
            {
                extracted = null;
            }
        }

        extracted ??= ExtractWithFallback(history, knownDestinations);
        ApplyDefaults(extracted);
        return extracted;
    }

    private async Task<TripRequirements?> ExtractWithLlmAsync(
        IReadOnlyList<ChatMessageDto> history,
        IReadOnlyList<string> knownDestinations,
        CancellationToken cancellationToken)
    {
        var transcript = string.Join("\n", history.Select(m => $"{m.Role}: {m.Content}"));
        var destinations = string.Join(", ", knownDestinations);
        var system = """
            You extract travel planning requirements from a conversation.
            Return JSON only with keys:
            destination, startDate (ISO date or null), endDate (ISO date or null),
            durationDays (int or null), travelers (int), budget (number or null),
            interests (string or null), accommodationPreference (string or null),
            missingFields (array of strings).
            Known catalog destinations: 
            """ + destinations + """
            Prefer matching a known destination name.
            Budget numbers like "Rs. 50,000" or "50000" are LKR amounts. Strip commas.
            If the user asks for an N-day trip, set durationDays to N.
            missingFields should list only destination and/or budget when those are unknown.
            """;

        var raw = await _llm.CompleteAsync(system, transcript, jsonMode: true, cancellationToken);
        raw = StripFence(raw);
        var parsed = JsonSerializer.Deserialize<TripRequirements>(raw, JsonOptions);
        return parsed;
    }

    public static TripRequirements ExtractWithFallback(
        IReadOnlyList<ChatMessageDto> history,
        IReadOnlyList<string> knownDestinations)
    {
        var text = string.Join(" ", history.Where(m => m.Role == "user").Select(m => m.Content));
        var req = new TripRequirements();

        foreach (var name in knownDestinations.OrderByDescending(n => n.Length))
        {
            if (text.Contains(name, StringComparison.OrdinalIgnoreCase))
            {
                req.Destination = name;
                break;
            }
        }

        var budgetMatch = Regex.Match(text, @"(?:rs\.?|lkr|usd|\$|budget|under|below)\s*:?\s*([\d,]+)", RegexOptions.IgnoreCase);
        if (!budgetMatch.Success)
            budgetMatch = Regex.Match(text, @"([\d,]+)\s*(?:rs\.?|lkr)", RegexOptions.IgnoreCase);
        if (budgetMatch.Success &&
            decimal.TryParse(budgetMatch.Groups[1].Value.Replace(",", ""), NumberStyles.Number, CultureInfo.InvariantCulture, out var budget))
        {
            req.Budget = budget;
        }

        var daysMatch = Regex.Match(text, @"(\d+)\s*-?\s*days?", RegexOptions.IgnoreCase);
        if (daysMatch.Success && int.TryParse(daysMatch.Groups[1].Value, out var days) && days > 0)
            req.DurationDays = days;

        var travelersMatch = Regex.Match(text, @"(\d+)\s*(?:travelers?|people|persons?|guests?)", RegexOptions.IgnoreCase);
        if (travelersMatch.Success && int.TryParse(travelersMatch.Groups[1].Value, out var travelers) && travelers > 0)
            req.Travelers = travelers;

        if (Regex.IsMatch(text, @"hike|hiking|nature|tea|waterfall|scenic", RegexOptions.IgnoreCase))
            req.Interests = "nature, hiking, sightseeing";
        else if (Regex.IsMatch(text, @"beach|surf|ocean", RegexOptions.IgnoreCase))
            req.Interests = "beach, relaxation";
        else if (Regex.IsMatch(text, @"culture|temple|heritage|history", RegexOptions.IgnoreCase))
            req.Interests = "culture, heritage";

        if (Regex.IsMatch(text, @"budget|cheap|hostel", RegexOptions.IgnoreCase))
            req.AccommodationPreference = "budget";
        else if (Regex.IsMatch(text, @"luxury|suite|5[\s-]?star", RegexOptions.IgnoreCase))
            req.AccommodationPreference = "luxury";
        else if (Regex.IsMatch(text, @"mid[\s-]?range|standard", RegexOptions.IgnoreCase))
            req.AccommodationPreference = "mid-range";

        return req;
    }

    public static void ApplyDefaults(TripRequirements req)
    {
        req.MissingFields.Clear();
        if (string.IsNullOrWhiteSpace(req.Destination))
            req.MissingFields.Add("destination");
        if (req.Budget is null or <= 0)
            req.MissingFields.Add("budget");

        if (req.Travelers <= 0)
            req.Travelers = 2;
        req.DurationDays ??= 3;

        var start = req.StartDate?.Date ?? DateTime.UtcNow.Date.AddDays(14);
        req.StartDate = DateTime.SpecifyKind(start, DateTimeKind.Utc);
        var duration = req.DurationDays.Value;
        req.EndDate = req.StartDate.Value.AddDays(Math.Max(duration - 1, 0));

        req.Interests ??= "nature, hiking, sightseeing";
        req.AccommodationPreference ??= "mid-range";
    }

    public string BuildClarifyingMessage(TripRequirements req)
    {
        if (req.MissingFields.Contains("destination") && req.MissingFields.Contains("budget"))
            return "I can plan that. Where would you like to go, and what is your total budget (for example Rs. 50,000)?";
        if (req.MissingFields.Contains("destination"))
            return "Happy to help. Which destination should I plan for? (for example Ella, Kandy, or Galle)";
        if (req.MissingFields.Contains("budget"))
            return "Great destination. What is your total budget for the trip?";
        return "Tell me the destination and budget and I will put a plan together.";
    }

    private static string StripFence(string raw)
    {
        raw = raw.Trim();
        if (!raw.StartsWith("```", StringComparison.Ordinal))
            return raw;
        var start = raw.IndexOf('\n');
        var end = raw.LastIndexOf("```", StringComparison.Ordinal);
        if (start >= 0 && end > start)
            return raw[(start + 1)..end].Trim();
        return raw;
    }
}
