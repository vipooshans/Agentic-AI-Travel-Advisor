using System.Globalization;
using System.Text.RegularExpressions;
using TravelAdvisor.Core.DTOs.AI;
using TravelAdvisor.Core.Enums;

namespace TravelAdvisor.Infrastructure.AI.Planning;

public sealed record BudgetMatch(decimal Amount, string Currency);

/// <summary>
/// Deterministic extraction of trip requirements from the user's messages. Later messages win, so a
/// user can refine one field ("make it 4 people") without repeating the rest.
/// </summary>
public static class RequirementParser
{
    public const int DefaultTravelers = 2;
    public const int DefaultDurationDays = 3;
    public const int DefaultLeadDays = 14;
    public const int MaxDurationDays = 30;
    public const int MaxTravelers = 20;

    private const RegexOptions Ci = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;

    private static readonly Dictionary<string, int> WordNumbers = new(StringComparer.OrdinalIgnoreCase)
    {
        ["one"] = 1, ["two"] = 2, ["three"] = 3, ["four"] = 4, ["five"] = 5, ["six"] = 6, ["seven"] = 7,
        ["eight"] = 8, ["nine"] = 9, ["ten"] = 10, ["eleven"] = 11, ["twelve"] = 12, ["thirteen"] = 13, ["fourteen"] = 14
    };

    private const string Num = @"(?<n>\d{1,2}|one|two|three|four|five|six|seven|eight|nine|ten|eleven|twelve|thirteen|fourteen)";

    private static readonly Regex AmountRegex = new(
        @"(?<![\d\-/:])(?<num>\d{1,3}(?:,\d{2,3})+(?:\.\d+)?|\d+(?:\.\d+)?)(?!\d)(?![\-/:]\d)\s*(?<mult>k\b|thousand\b|lakhs?\b|lacs?\b|mn\b|million\b)?",
        Ci);

    private static readonly Regex NonMoneyUnit = new(
        @"^\s*-?\s*(days?|nights?|people|persons?|pax|adults?|kids?|child(ren)?|travell?ers?|guests?|stars?|km|kms|kilomet(er|re)s?|hours?|hrs?|mins?|minutes?|am|pm|rooms?|weeks?|months?|years?|yrs?|of\s+us|friends|st|nd|rd|th|%)\b",
        Ci);

    private static readonly Regex BudgetKeywordBefore = new(
        @"(budget|under|below|within|max(imum)?|up\s*to|less\s+than|spend|around|about|approx(imately)?|total|cost|afford|limit|no\s+more\s+than|cap)\b[^\d]{0,20}$",
        Ci);

    private static readonly Regex BudgetKeywordAfter = new(@"^\s*(budget|total|max(imum)?|in\s+total|all\s+in)\b", Ci);

    private static readonly (Regex Pattern, string Currency)[] CurrencyBefore =
    [
        (new Regex(@"(rs\.?|lkr|₨|රු\.?)\s*$", Ci), "LKR"),
        (new Regex(@"(usd|us\$|\$)\s*$", Ci), "USD"),
        (new Regex(@"(eur|€)\s*$", Ci), "EUR"),
        (new Regex(@"(gbp|£)\s*$", Ci), "GBP")
    ];

    private static readonly (Regex Pattern, string Currency)[] CurrencyAfter =
    [
        (new Regex(@"^\s*(rs\.?|lkr|rupees?|/-)", Ci), "LKR"),
        (new Regex(@"^\s*(usd|dollars?|bucks)\b", Ci), "USD"),
        (new Regex(@"^\s*(eur|euros?)\b", Ci), "EUR"),
        (new Regex(@"^\s*(gbp|pounds?|quid)\b", Ci), "GBP")
    ];

    private static readonly (string Category, Regex Pattern)[] InterestRules =
    [
        ("nature", new Regex(@"\b(nature|scenic|tea|waterfalls?|gardens?|views?|green|mountains?|hill\s*country)\b", Ci)),
        ("hiking", new Regex(@"\b(hik(e|es|ing)|trek(s|king)?|climb(ing)?|walks?)\b", Ci)),
        ("culture", new Regex(@"\b(cultur(e|al)|temples?|heritage|histor(y|ic|ical)|museums?|forts?)\b", Ci)),
        ("beach", new Regex(@"\b(beach(es)?|surf(ing)?|ocean|sea|swim(ming)?|snorkel(l?ing)?)\b", Ci)),
        ("adventure", new Regex(@"\b(adventure|rafting|zip\s*-?line|safari|canyoning)\b", Ci)),
        ("food", new Regex(@"\b(food|culinary|cuisine|tasting|street\s+food|restaurants?)\b", Ci)),
        ("sightseeing", new Regex(@"\b(sightseeing|sights|photography|viewpoints?|landmarks?)\b", Ci))
    ];

    private static readonly Regex Months = new(@"^(jan|feb|mar|apr|may|jun|jul|aug|sep|sept|oct|nov|dec)", Ci);

    public static TripRequirements Parse(
        IReadOnlyList<ChatMessageDto> history,
        IReadOnlyList<string> knownDestinations,
        DateOnly today,
        IReadOnlyDictionary<string, decimal>? lkrPerUnit = null)
    {
        var latestFirst = history
            .Where(m => m.Role == "user" && !string.IsNullOrWhiteSpace(m.Content))
            .Select(m => m.Content)
            .Reverse()
            .ToList();

        var req = new TripRequirements { Travelers = 0 };

        req.Destination = FirstMatch(latestFirst, msg => FindDestination(msg, knownDestinations));

        var budget = FirstMatch(latestFirst, ParseBudget);
        if (budget is not null)
            ApplyBudget(req, budget, lkrPerUnit);

        var dates = FirstValue(latestFirst, msg => ParseDates(msg, today, req.Warnings));
        if (dates is not null)
        {
            req.StartDate = ToUtc(dates.Value.Start);
            if (dates.Value.End is { } end)
                req.DurationDays = end.DayNumber - dates.Value.Start.DayNumber + 1;
        }

        var days = FirstValue(latestFirst, ParseDurationDays);
        if (days is not null)
            req.DurationDays = days;

        req.Travelers = FirstValue(latestFirst, ParseTravelers) ?? 0;
        req.TransportPreference = FirstValue(latestFirst, ParseTransportMode)?.ToString();
        req.Origin = FirstMatch(latestFirst, msg => ParseOrigin(msg, req.Destination));
        req.AccommodationPreference = FirstMatch(latestFirst, ParseAccommodation);

        var interests = latestFirst.SelectMany(ParseInterests).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (interests.Count > 0)
            req.Interests = string.Join(", ", interests);

        return req;
    }

    /// <summary>Fills defaults, records every assumption, and lists what is still missing.</summary>
    public static void ApplyDefaults(TripRequirements req, DateOnly today)
    {
        req.MissingFields.Clear();
        if (string.IsNullOrWhiteSpace(req.Destination))
            req.MissingFields.Add("destination");
        if (req.Budget is null or <= 0)
            req.MissingFields.Add("budget");

        if (req.Travelers <= 0)
        {
            req.Travelers = DefaultTravelers;
            AddOnce(req.Assumptions, $"Assumed {DefaultTravelers} travelers because the number of people was not given.");
        }
        else if (req.Travelers > MaxTravelers)
        {
            AddOnce(req.Warnings, $"Group size was capped at {MaxTravelers} travelers.");
            req.Travelers = MaxTravelers;
        }

        if (req.DurationDays is null or <= 0)
        {
            req.DurationDays = DefaultDurationDays;
            AddOnce(req.Assumptions, $"Assumed a {DefaultDurationDays}-day trip because no duration was given.");
        }
        else if (req.DurationDays > MaxDurationDays)
        {
            AddOnce(req.Warnings, $"Trips are limited to {MaxDurationDays} days, so the plan covers the first {MaxDurationDays}.");
            req.DurationDays = MaxDurationDays;
        }

        if (req.StartDate is null)
        {
            req.StartDate = ToUtc(today.AddDays(DefaultLeadDays));
            AddOnce(req.Assumptions, $"Assumed a start date of {today.AddDays(DefaultLeadDays):yyyy-MM-dd} (two weeks from today).");
        }
        else
        {
            req.StartDate = DateTime.SpecifyKind(req.StartDate.Value.Date, DateTimeKind.Utc);
        }

        req.EndDate = req.StartDate.Value.AddDays(req.DurationDays.Value - 1);

        if (string.IsNullOrWhiteSpace(req.Interests))
        {
            req.Interests = "sightseeing";
            AddOnce(req.Assumptions, "Assumed general sightseeing interests.");
        }

        if (string.IsNullOrWhiteSpace(req.AccommodationPreference))
            req.AccommodationPreference = "mid-range";

        req.Currency = "LKR";
    }

    public static BudgetMatch? ParseBudget(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return null;

        var bareNumber = Regex.IsMatch(message, @"^\s*(rs\.?|lkr|\$|usd)?\s*\d[\d,]*(\.\d+)?\s*(k|lakhs?)?\s*(rs\.?|lkr|rupees|usd|dollars)?\s*\.?\s*$", Ci);
        BudgetMatch? best = null;
        var bestScore = 0;

        foreach (Match match in AmountRegex.Matches(message))
        {
            if (!decimal.TryParse(match.Groups["num"].Value.Replace(",", ""), NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
                continue;

            var multiplier = match.Groups["mult"].Value.ToLowerInvariant() switch
            {
                "k" or "thousand" => 1_000m,
                "lakh" or "lakhs" or "lac" or "lacs" => 100_000m,
                "mn" or "million" => 1_000_000m,
                _ => 1m
            };
            value *= multiplier;

            var before = message[Math.Max(0, match.Index - 30)..match.Index];
            var after = message[(match.Index + match.Length)..Math.Min(message.Length, match.Index + match.Length + 20)];

            var currency = CurrencyBefore.FirstOrDefault(c => c.Pattern.IsMatch(before)).Currency
                           ?? CurrencyAfter.FirstOrDefault(c => c.Pattern.IsMatch(after)).Currency;

            if (currency is null && multiplier == 1m && NonMoneyUnit.IsMatch(after))
                continue;

            var score = (currency is not null ? 2 : 0)
                        + (BudgetKeywordBefore.IsMatch(before) || BudgetKeywordAfter.IsMatch(after) ? 1 : 0);
            if (score == 0 && !bareNumber)
                continue;

            var minimum = currency is null or "LKR" ? 1000m : 10m;
            if (value < minimum)
                continue;

            if (score > bestScore)
            {
                best = new BudgetMatch(value, currency ?? "LKR");
                bestScore = score;
            }
            else if (bareNumber && best is null)
            {
                best = new BudgetMatch(value, currency ?? "LKR");
            }
        }

        return best;
    }

    public static void ApplyBudget(TripRequirements req, BudgetMatch budget, IReadOnlyDictionary<string, decimal>? lkrPerUnit)
    {
        if (budget.Currency == "LKR")
        {
            req.Budget = budget.Amount;
            req.OriginalBudget = null;
            req.OriginalCurrency = null;
            return;
        }

        req.OriginalBudget = budget.Amount;
        req.OriginalCurrency = budget.Currency;
        if (lkrPerUnit is not null && lkrPerUnit.TryGetValue(budget.Currency, out var rate) && rate > 0)
        {
            req.Budget = Math.Round(budget.Amount * rate, 0);
            AddOnce(req.Assumptions,
                $"Converted your budget of {budget.Currency} {budget.Amount:N0} to Rs. {req.Budget:N0} at an indicative {rate:N0} LKR per {budget.Currency}; catalog prices are in LKR.");
        }
        else
        {
            req.Budget = null;
            AddOnce(req.Warnings, $"I can't convert {budget.Currency} yet, so please give your budget in LKR.");
        }
    }

    public static int? ParseDurationDays(string message)
    {
        var days = Regex.Match(message, Num + @"\s*-?\s*days?\b", Ci);
        if (days.Success)
            return ToInt(days.Groups["n"].Value);

        var nights = Regex.Match(message, Num + @"\s*-?\s*nights?\b", Ci);
        if (nights.Success)
            return ToInt(nights.Groups["n"].Value) + 1;

        var weeks = Regex.Match(message, @"\b(?<n>a|one|1|two|2)\s*-?\s*weeks?\b", Ci);
        if (weeks.Success)
            return weeks.Groups["n"].Value.ToLowerInvariant() is "two" or "2" ? 14 : 7;

        if (Regex.IsMatch(message, @"\bweekend\b", Ci))
            return 2;

        return null;
    }

    public static int? ParseTravelers(string message)
    {
        var count = Regex.Match(message, Num + @"\s*(adults?|travell?ers?|people|persons?|pax|guests?|of\s+us|members)\b", Ci);
        if (count.Success)
            return ToInt(count.Groups["n"].Value);

        var friends = Regex.Match(message, @"\bwith\s+" + Num + @"\s+(friends|colleagues|others)\b", Ci);
        if (friends.Success)
            return ToInt(friends.Groups["n"].Value) + 1;

        var family = Regex.Match(message, @"\bfamily\s+of\s+" + Num + @"\b", Ci);
        if (family.Success)
            return ToInt(family.Groups["n"].Value);

        if (Regex.IsMatch(message, @"\b(couple|honeymoon|two\s+of\s+us|me\s+and\s+my\s+(wife|husband|partner|girlfriend|boyfriend|friend)|my\s+(wife|husband|partner)\s+and\s+(i|me))\b", Ci))
            return 2;

        if (Regex.IsMatch(message, @"\b(solo|alone|just\s+me|by\s+myself|only\s+me)\b", Ci))
            return 1;

        var forN = Regex.Match(message, @"\bfor\s+" + Num + @"\b(?!\s*(-|days?|nights?|weeks?|k\b|lakhs?|,\d|\d))", Ci);
        if (forN.Success)
            return ToInt(forN.Groups["n"].Value);

        return null;
    }

    public static TransportMode? ParseTransportMode(string message)
    {
        var match = Regex.Match(message,
            @"\b(by|via|take\s+(a\s+|the\s+)?|prefer(ably)?\s+(a\s+|the\s+)?|travel(l?ing)?\s+by)\s*(?<m>train|rail|bus|coach|car|taxi|cab|van|tuk[\s-]?tuks?|flight|plane|air|ferry|boat)\b",
            Ci);
        if (!match.Success)
            match = Regex.Match(message, @"\b(?<m>train|bus|van|tuk[\s-]?tuk|ferry)\s+(ride|journey|travel|trip|transfer)\b", Ci);
        if (!match.Success)
            return null;

        var mode = match.Groups["m"].Value.ToLowerInvariant();
        return mode switch
        {
            "train" or "rail" => TransportMode.Train,
            "bus" or "coach" => TransportMode.Bus,
            "car" or "taxi" or "cab" => TransportMode.Car,
            "van" => TransportMode.Van,
            "flight" or "plane" or "air" => TransportMode.Flight,
            "ferry" or "boat" => TransportMode.Ferry,
            _ when mode.StartsWith("tuk") => TransportMode.TukTuk,
            _ => null
        };
    }

    public static string? ParseOrigin(string message, string? destination)
    {
        foreach (Match match in Regex.Matches(message, @"\b[Ff]rom\s+(?<o>[A-Z][a-zA-Z]+(?:\s+[A-Z][a-zA-Z]+)?)"))
        {
            var origin = match.Groups["o"].Value.Trim();
            var first = origin.Split(' ')[0];
            if (Months.IsMatch(first) && first.Length <= 9)
                continue;
            if (destination is not null && origin.Equals(destination, StringComparison.OrdinalIgnoreCase))
                continue;
            return origin;
        }

        return null;
    }

    public static string? ParseAccommodation(string message)
    {
        if (Regex.IsMatch(message, @"\b(budget|cheap|affordable|basic)\s+(hotels?|stays?|accommodation|rooms?|options?|guest\s*houses?)\b|\bcheap\b|\bhostels?\b", Ci))
            return "budget";
        if (Regex.IsMatch(message, @"\b(luxury|luxurious|suites?|5[\s-]?star|five[\s-]star|premium)\b", Ci))
            return "luxury";
        if (Regex.IsMatch(message, @"\bmid[\s-]?range\b|\bstandard\b|\b(3|4)[\s-]?star\b", Ci))
            return "mid-range";
        return null;
    }

    public static IEnumerable<string> ParseInterests(string message) =>
        InterestRules.Where(r => r.Pattern.IsMatch(message)).Select(r => r.Category);

    public static IReadOnlyList<string> SplitInterests(string? interests) =>
        string.IsNullOrWhiteSpace(interests)
            ? []
            : interests.Split([',', ';', '/'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(i => i.ToLowerInvariant())
                .Distinct()
                .ToList();

    public static (DateOnly Start, DateOnly? End)? ParseDates(string message, DateOnly today, List<string>? warnings = null)
    {
        var found = new List<(int Index, DateOnly Date)>();

        foreach (Match m in Regex.Matches(message, @"\b(?<y>20\d{2})-(?<mo>\d{1,2})-(?<d>\d{1,2})\b"))
        {
            if (TryDate(int.Parse(m.Groups["y"].Value), int.Parse(m.Groups["mo"].Value), int.Parse(m.Groups["d"].Value), out var date))
                found.Add((m.Index, date));
        }

        const string month = @"(?<mon>jan|feb|mar|apr|may|jun|jul|aug|sep|sept|oct|nov|dec)[a-z]*\.?";
        foreach (Match m in Regex.Matches(message, @"\b(?<d>\d{1,2})(st|nd|rd|th)?\s+(of\s+)?" + month + @"(,?\s+(?<y>20\d{2}))?\b", Ci))
            AddMonthDate(m, today, found);
        foreach (Match m in Regex.Matches(message, @"\b" + month + @"\s+(?<d>\d{1,2})(st|nd|rd|th)?(,?\s+(?<y>20\d{2}))?\b", Ci))
            AddMonthDate(m, today, found);

        if (Regex.IsMatch(message, @"\btomorrow\b", Ci))
            found.Add((message.IndexOf("tomorrow", StringComparison.OrdinalIgnoreCase), today.AddDays(1)));
        else if (Regex.IsMatch(message, @"\bnext\s+weekend\b", Ci))
            found.Add((0, NextSaturday(today).AddDays(7)));
        else if (Regex.IsMatch(message, @"\bthis\s+weekend\b", Ci))
            found.Add((0, NextSaturday(today)));
        else if (Regex.IsMatch(message, @"\bnext\s+week\b", Ci))
            found.Add((0, today.AddDays(7)));
        else if (Regex.IsMatch(message, @"\bnext\s+month\b", Ci))
            found.Add((0, new DateOnly(today.Year, today.Month, 1).AddMonths(1)));

        if (found.Count == 0)
            return null;

        var ordered = found.DistinctBy(f => f.Date).OrderBy(f => f.Index).Select(f => f.Date).ToList();
        var start = ordered[0];
        if (start < today)
        {
            warnings?.Add($"The date {start:yyyy-MM-dd} is in the past, so I used the default start date instead.");
            return null;
        }

        DateOnly? end = null;
        foreach (var candidate in ordered.Skip(1))
        {
            if (candidate > start)
            {
                end = candidate;
                break;
            }
        }

        return (start, end);
    }

    private static void AddMonthDate(Match m, DateOnly today, List<(int, DateOnly)> found)
    {
        var monthIndex = Array.FindIndex(
            ["jan", "feb", "mar", "apr", "may", "jun", "jul", "aug", "sep", "oct", "nov", "dec"],
            name => m.Groups["mon"].Value.StartsWith(name, StringComparison.OrdinalIgnoreCase)) + 1;
        if (monthIndex <= 0 || !int.TryParse(m.Groups["d"].Value, out var day))
            return;

        var explicitYear = m.Groups["y"].Success;
        var year = explicitYear ? int.Parse(m.Groups["y"].Value) : today.Year;
        if (!TryDate(year, monthIndex, day, out var date))
            return;
        if (!explicitYear && date < today)
            TryDate(year + 1, monthIndex, day, out date);
        found.Add((m.Index, date));
    }

    private static string? FindDestination(string message, IReadOnlyList<string> knownDestinations)
    {
        foreach (var name in knownDestinations.OrderByDescending(n => n.Length))
        {
            if (Regex.IsMatch(message, $@"\b{Regex.Escape(name)}\b", Ci))
                return name;
        }

        return null;
    }

    private static T? FirstMatch<T>(IEnumerable<string> messages, Func<string, T?> parse) where T : class
    {
        foreach (var message in messages)
        {
            var value = parse(message);
            if (value is not null)
                return value;
        }

        return null;
    }

    private static T? FirstValue<T>(IEnumerable<string> messages, Func<string, T?> parse) where T : struct
    {
        foreach (var message in messages)
        {
            var value = parse(message);
            if (value is not null)
                return value;
        }

        return null;
    }

    private static int ToInt(string value) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : WordNumbers.GetValueOrDefault(value, 0);

    private static bool TryDate(int year, int month, int day, out DateOnly date)
    {
        date = default;
        if (month is < 1 or > 12 || day < 1 || day > DateTime.DaysInMonth(year, month))
            return false;
        date = new DateOnly(year, month, day);
        return true;
    }

    private static DateOnly NextSaturday(DateOnly today)
    {
        var offset = ((int)DayOfWeek.Saturday - (int)today.DayOfWeek + 7) % 7;
        return today.AddDays(offset == 0 ? 7 : offset);
    }

    private static DateTime ToUtc(DateOnly date) => DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

    private static void AddOnce(List<string> list, string text)
    {
        if (!list.Contains(text))
            list.Add(text);
    }
}
