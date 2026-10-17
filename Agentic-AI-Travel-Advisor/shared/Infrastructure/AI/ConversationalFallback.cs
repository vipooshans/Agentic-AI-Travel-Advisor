using System.Globalization;
using System.Text.RegularExpressions;
using TravelAdvisor.Core.DTOs.Bookings;

namespace TravelAdvisor.Infrastructure.AI;

public enum FallbackStep
{
    Greeting,
    Thanks,
    Suggest,
    ConfirmHint,
    Chat,
    AskLocation,
    AskDates,
    AskGuests,
    Ready
}

public sealed record FallbackSlots(string? Location, DateTime? CheckIn, DateTime? CheckOut, int? Guests);

public sealed record FallbackDecision(
    FallbackStep Step,
    string Message,
    FallbackSlots Slots,
    List<BookingSuggestionDto> Suggestions);

public static class ConversationalFallback
{
    private const RegexOptions Ci = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
    private static readonly Regex BookingWords = new(@"\b(book|booking|hotel|hotels|room|stay|reserve|reservation|tour package|package)\b", Ci);
    private static readonly Regex SuggestWords = new(@"\b(suggest|recommend|recommendation|ideas|inspiration|where should i)\b", Ci);
    private static readonly Regex ThanksWords = new(@"^\s*(thanks?|thank you|thx|cheers)\b", Ci);
    private static readonly Regex ConfirmWords = new(@"^\s*(yes|yeah|yep|ok|okay|confirm|confirmed|please confirm)(?:\s+please)?[.! ]*$", Ci);
    private static readonly Regex GuestCount = new(@"\b(?<count>\d{1,2})\s*(?:guests?|people|travelers?|travellers?|pax|persons?)\b|\bfamily of\s+(?<family>\d{1,2})\b|\bfor\s+(?<for>\d{1,2})(?!\s*nights?)\b", Ci);
    private static readonly Regex Nights = new(@"\bfor\s+(?<nights>\d{1,3})\s+nights?\b", Ci);
    private static readonly Regex IsoRange = new(@"\b(?<y1>\d{4})-(?<m1>\d{1,2})-(?<d1>\d{1,2})\s*(?:to|until|through|-|and)\s*(?:(?<y2>\d{4})-)?(?<m2>\d{1,2})-(?<d2>\d{1,2})\b", Ci);
    private static readonly Regex IsoSingle = new(@"\b(?<year>\d{4})-(?<month>\d{1,2})-(?<day>\d{1,2})\b", Ci);
    private static readonly Regex MonthRange = new(@"\b(?<startmonth>january|february|march|april|may|june|july|august|september|october|november|december|jan|feb|mar|apr|jun|jul|aug|sep|sept|oct|nov|dec)\.?\s+(?<start>\d{1,2})(?:st|nd|rd|th)?\s*(?:to|until|through|-|and)\s*(?<endmonth>january|february|march|april|may|june|july|august|september|october|november|december|jan|feb|mar|apr|jun|jul|aug|sep|sept|oct|nov|dec)\.?\s+(?<end>\d{1,2})(?:st|nd|rd|th)?(?:,?\s*(?<year>20\d{2}))?", Ci);
    private static readonly Regex MonthFirst = new(@"\b(?<month>january|february|march|april|may|june|july|august|september|october|november|december|jan|feb|mar|apr|jun|jul|aug|sep|sept|oct|nov|dec)\.?\s+(?<day>\d{1,2})(?:st|nd|rd|th)?(?:,?\s*(?<year>20\d{2}))?(?:\s*(?:to|until|through|-|and)\s*(?<end>\d{1,2})(?:st|nd|rd|th)?(?:,?\s*(?<endyear>20\d{2}))?)?", Ci);
    private static readonly Regex DayFirst = new(@"\b(?<day>\d{1,2})(?:st|nd|rd|th)?\s+(?<month>january|february|march|april|may|june|july|august|september|october|november|december|jan|feb|mar|apr|jun|jul|aug|sep|sept|oct|nov|dec)\.?(?:\s*(?:to|until|through|-|and)\s*(?<end>\d{1,2})(?:st|nd|rd|th)?(?:\s+(?<endmonth>january|february|march|april|may|june|july|august|september|october|november|december|jan|feb|mar|apr|jun|jul|aug|sep|sept|oct|nov|dec)\.?)?)?(?:,?\s*(?<year>20\d{2}))?", Ci);
    private static readonly Regex DayRange = new(@"\b(?<start>\d{1,2})(?:st|nd|rd|th)?\s*(?:to|until|through|-|and)\s*(?<end>\d{1,2})(?:st|nd|rd|th)?\b", Ci);
    private static readonly Regex SingleOrdinal = new(@"\b(?<day>\d{1,2})(?:st|nd|rd|th)\b", Ci);
    private static readonly Regex SingleDay = new(@"^\s*(?:on\s+)?(?<day>\d{1,2})\s*[.!]?\s*$", Ci);
    private static readonly Regex Destinations = BuildDestinationRegex();

    private static readonly string[] MonthNames =
    [
        "january", "february", "march", "april", "may", "june",
        "july", "august", "september", "october", "november", "december"
    ];

    private static readonly (string Name, string Reason)[] PopularDestinations =
    [
        ("Galle", "a historic fort, ocean views, and relaxed cafés"),
        ("Ella", "cool hill-country air, tea trails, and scenic hikes"),
        ("Kandy", "lake-side culture and easy access to the central highlands"),
        ("Mirissa", "southern beaches, sunsets, and seasonal whale watching")
    ];

    public static FallbackDecision Decide(IReadOnlyList<BookingAgentMessageDto> history, DateTime today)
    {
        var all = history.ToList();
        var currentUserIndex = all.FindLastIndex(message => message.Role == "user");
        var currentPrompt = currentUserIndex >= 0 ? all[currentUserIndex].Content.Trim() : string.Empty;
        var previousMessage = currentUserIndex > 0 ? all[currentUserIndex - 1] : null;
        var proposalIndex = all.FindLastIndex(message => message.Role == "assistant" && message.Proposal != null);

        if (proposalIndex >= 0
            && currentUserIndex > proposalIndex
            && currentUserIndex == all.Count - 1
            && previousMessage is { Role: "assistant", Proposal: not null }
            && ConfirmWords.IsMatch(currentPrompt))
        {
            return Decision(FallbackStep.ConfirmHint,
                "Your quote is ready. Tap **Review & Reserve** on the proposal card above to check the details and submit it. I won't place a booking from chat.");
        }

        var relevant = proposalIndex >= 0
            ? all.Skip(proposalIndex + 1).ToList()
            : all;
        var recent = relevant.TakeLast(12).ToList();
        currentUserIndex = recent.FindLastIndex(message => message.Role == "user");
        currentPrompt = currentUserIndex >= 0 ? recent[currentUserIndex].Content.Trim() : string.Empty;
        var previousAssistant = currentUserIndex > 0
            ? recent.Take(currentUserIndex).LastOrDefault(message => message.Role == "assistant")
            : null;
        var assistantAskedGuests = previousAssistant != null
            && Regex.IsMatch(previousAssistant.Content, @"\b(guest|people|travelers?|travellers?|pax)\b", Ci);
        var assistantAskedDates = previousAssistant != null
            && Regex.IsMatch(previousAssistant.Content, @"\b(date|check[\s-]?in|check[\s-]?out|when|night)\b", Ci);
        var assistantAskedCheckout = previousAssistant != null
            && Regex.IsMatch(previousAssistant.Content, @"\b(check[\s-]?out|how many nights?)\b", Ci);

        if (SuggestWords.IsMatch(currentPrompt))
        {
            var ideas = string.Join(Environment.NewLine, PopularDestinations.Select(destination => $"• **{destination.Name}** — {destination.Reason}."));
            return Decision(FallbackStep.Suggest,
                $"Here are a few lovely places to start:\n{ideas}\nWhich one sounds most like your kind of trip?",
                suggestions: DestinationSuggestions());
        }

        if (ThanksWords.IsMatch(currentPrompt))
            return Decision(FallbackStep.Thanks, "You're very welcome! Tell me where you'd like to go and I'll help you plan a stay.");

        if (IsGreeting(currentPrompt) && !BookingWords.IsMatch(currentPrompt))
        {
            return Decision(FallbackStep.Greeting,
                "Hi! I can help you book hotels and tour packages across Sri Lanka. Where would you like to go?",
                suggestions: GreetingSuggestions());
        }

        var slots = new FallbackSlots(null, null, null, null);
        string? dateError = null;
        string? guestError = null;
        DateTime todayDate = today.Date;

        for (var index = 0; index < recent.Count; index++)
        {
            var message = recent[index];
            if (message.Role != "user")
                continue;

            var location = FindDestination(message.Content);
            if (location != null)
                slots = slots with { Location = location };

            var assistantBeforeMessage = index > 0
                ? recent.Take(index).LastOrDefault(item => item.Role == "assistant")
                : null;
            var askedDatesForMessage = assistantBeforeMessage != null
                && Regex.IsMatch(assistantBeforeMessage.Content, @"\b(date|check[\s-]?in|check[\s-]?out|when|night)\b", Ci);
            var askedCheckoutForMessage = assistantBeforeMessage != null
                && Regex.IsMatch(assistantBeforeMessage.Content, @"\b(check[\s-]?out|how many nights?)\b", Ci);
            var parsedDates = ParseDates(message.Content, todayDate, slots.CheckIn, askedDatesForMessage, askedCheckoutForMessage);
            if (parsedDates.Error != null)
                dateError = parsedDates.Error;
            else if (parsedDates.HasDate)
            {
                dateError = null;
                if (parsedDates.CheckIn.HasValue)
                    slots = slots with { CheckIn = parsedDates.CheckIn, CheckOut = parsedDates.CheckOut };
                else if (parsedDates.CheckOut.HasValue)
                    slots = slots with { CheckOut = parsedDates.CheckOut };
            }

            var allowBareGuestCount = index == currentUserIndex && assistantAskedGuests;
            var parsedGuests = ParseGuests(message.Content, allowBareGuestCount);
            if (parsedGuests.Invalid)
            {
                guestError = "Please choose a guest count from 1 to 50.";
                slots = slots with { Guests = null };
            }
            else if (parsedGuests.Guests.HasValue)
            {
                guestError = null;
                slots = slots with { Guests = parsedGuests.Guests };
            }
        }

        if (dateError != null)
            return Decision(FallbackStep.AskDates, dateError, slots);
        if (guestError != null)
            return Decision(FallbackStep.AskGuests, guestError, slots);

        var hasBookingContext = recent
            .Where(message => message.Role == "user")
            .Any(message => BookingWords.IsMatch(message.Content))
            || previousAssistant != null && (assistantAskedGuests || assistantAskedDates
                || Regex.IsMatch(previousAssistant.Content, @"\b(where|destination|visit|go)\b", Ci))
            || slots.Location != null || slots.CheckIn.HasValue || slots.CheckOut.HasValue || slots.Guests.HasValue;

        if (!hasBookingContext)
            return Decision(FallbackStep.Chat, "I can help you discover places, find a hotel, or plan a Sri Lankan tour. What would you like to explore?");

        if (string.IsNullOrWhiteSpace(slots.Location))
            return Decision(FallbackStep.AskLocation, "Where in Sri Lanka would you like to stay or visit?", slots);
        if (!slots.CheckIn.HasValue)
            return Decision(FallbackStep.AskDates, $"Lovely choice! What check-in and check-out dates would work for {slots.Location}?", slots);
        if (!slots.CheckOut.HasValue)
            return Decision(FallbackStep.AskDates, "What check-out date would you prefer, or how many nights would you like to stay?", slots);
        if (!slots.Guests.HasValue)
            return Decision(FallbackStep.AskGuests, "And how many guests will be traveling (1 to 50)?", slots);

        return Decision(FallbackStep.Ready, string.Empty, slots);
    }

    private static FallbackDecision Decision(
        FallbackStep step,
        string message,
        FallbackSlots? slots = null,
        List<BookingSuggestionDto>? suggestions = null) =>
        new(step, message, slots ?? new FallbackSlots(null, null, null, null), suggestions ?? []);

    private static List<BookingSuggestionDto> GreetingSuggestions() =>
    [
        new() { Label = "Hotel in Galle", Prompt = "I want a hotel in Galle", Type = "place_change" },
        new() { Label = "Hotel in Ella", Prompt = "I want a hotel in Ella", Type = "place_change" },
        new() { Label = "Hotel in Kandy", Prompt = "I want a hotel in Kandy", Type = "place_change" },
        new() { Label = "Suggest something", Prompt = "Suggest me somewhere to visit", Type = "general" }
    ];

    private static List<BookingSuggestionDto> DestinationSuggestions() =>
        PopularDestinations.Select(destination => new BookingSuggestionDto
        {
            Label = destination.Name,
            Prompt = $"I want to visit {destination.Name}",
            Type = "place_change"
        }).ToList();

    private static bool IsGreeting(string prompt) =>
        Regex.IsMatch(prompt, @"^\s*(hi|hello|hey|hai|vanakkam|good morning|good afternoon|good evening)\b[!.?, ]*$", Ci);

    private static string? FindDestination(string prompt)
    {
        var match = Destinations.Match(prompt);
        return match.Success
            ? CultureInfo.InvariantCulture.TextInfo.ToTitleCase(match.Groups["destination"].Value.ToLowerInvariant())
            : null;
    }

    private static Regex BuildDestinationRegex()
    {
        string[] destinations =
        [
            "Nuwara Eliya", "Arugam Bay", "Trincomalee", "Unawatuna", "Hikkaduwa",
            "Anuradhapura", "Polonnaruwa", "Batticaloa", "Bentota", "Negombo",
            "Sigiriya", "Colombo", "Mirissa", "Jaffna", "Galle", "Ella", "Kandy",
            "Yala", "Dambulla", "Tangalle", "Pasikuda", "Haputale", "Kalpitiya",
            "Badulla", "Matara", "Habarana", "Udawalawe", "Pinnawala"
        ];
        var alternatives = string.Join("|", destinations
            .OrderByDescending(destination => destination.Length)
            .Select(Regex.Escape));
        return new Regex($@"\b(?<destination>{alternatives})\b", Ci);
    }

    private static ParsedDates ParseDates(
        string prompt,
        DateTime today,
        DateTime? existingCheckIn,
        bool assistantAskedDates,
        bool assistantAskedCheckout)
    {
        var text = prompt.Trim();
        var nightsMatch = Nights.Match(text);
        var nightCount = nightsMatch.Success && int.TryParse(nightsMatch.Groups["nights"].Value, out var nights)
            ? nights
            : (int?)null;

        if (IsoRange.Match(text) is { Success: true } iso)
        {
            if (!TryCreateDate(iso.Groups["y1"].Value, iso.Groups["m1"].Value, iso.Groups["d1"].Value, out var checkIn)
                || !TryCreateDate(
                    iso.Groups["y2"].Success ? iso.Groups["y2"].Value : iso.Groups["y1"].Value,
                    iso.Groups["m2"].Value,
                    iso.Groups["d2"].Value,
                    out var checkOut))
                return ParsedDates.Invalid("I couldn't read those dates. What future check-in and check-out dates would you like?");
            return ValidateDates(checkIn, checkOut, today);
        }

        if (IsoSingle.Match(text) is { Success: true } isoSingle)
        {
            if (!TryCreateDate(isoSingle.Groups["year"].Value, isoSingle.Groups["month"].Value, isoSingle.Groups["day"].Value, out var date))
                return ParsedDates.Invalid("I couldn't read that date. What future date would you like?");
            if (assistantAskedCheckout && existingCheckIn.HasValue)
                return ValidateDates(existingCheckIn.Value, date, today);
            return ValidateDates(date, nightCount.HasValue ? date.AddDays(nightCount.Value) : null, today);
        }

        if (MonthRange.Match(text) is { Success: true } monthRange)
        {
            var year = monthRange.Groups["year"].Success
                ? int.Parse(monthRange.Groups["year"].Value, CultureInfo.InvariantCulture)
                : today.Year;
            var startMonth = MonthNumber(monthRange.Groups["startmonth"].Value);
            var endMonth = MonthNumber(monthRange.Groups["endmonth"].Value);
            if (!TryCreateDate(year, startMonth, int.Parse(monthRange.Groups["start"].Value, CultureInfo.InvariantCulture), out var checkIn))
                return ParsedDates.Invalid("That check-in date doesn't look valid. What future date would you like?");
            var endYear = endMonth < startMonth ? year + 1 : year;
            if (!TryCreateDate(endYear, endMonth, int.Parse(monthRange.Groups["end"].Value, CultureInfo.InvariantCulture), out var checkOut))
                return ParsedDates.Invalid("That check-out date doesn't look valid. What date should I use?");
            return ValidateDates(checkIn, checkOut, today);
        }

        if (Regex.IsMatch(text, @"\bday after tomorrow\b", Ci))
        {
            var checkIn = today.AddDays(2);
            return ParsedDates.Valid(checkIn, nightCount.HasValue ? checkIn.AddDays(nightCount.Value) : null);
        }
        if (Regex.IsMatch(text, @"\btomorrow\b", Ci))
        {
            var checkIn = today.AddDays(1);
            return ParsedDates.Valid(checkIn, nightCount.HasValue ? checkIn.AddDays(nightCount.Value) : null);
        }
        if (Regex.IsMatch(text, @"\btoday\b", Ci))
            return ParsedDates.Valid(today, nightCount.HasValue ? today.AddDays(nightCount.Value) : null);
        if (Regex.IsMatch(text, @"\bweekend\b", Ci))
        {
            var daysUntilFriday = ((int)DayOfWeek.Friday - (int)today.DayOfWeek + 7) % 7;
            var checkIn = today.AddDays(daysUntilFriday);
            return ParsedDates.Valid(checkIn, checkIn.AddDays(2));
        }

        var monthContext = Regex.Match(text, @"\b(this|next)\s+month\b", Ci);
        var requestedMonth = monthContext.Success
            ? today.AddMonths(monthContext.Groups[1].Value.Equals("next", StringComparison.OrdinalIgnoreCase) ? 1 : 0).Month
            : (int?)null;
        var requestedYear = monthContext.Success
            ? today.AddMonths(monthContext.Groups[1].Value.Equals("next", StringComparison.OrdinalIgnoreCase) ? 1 : 0).Year
            : today.Year;

        var named = MonthFirst.Match(text);
        bool dayFirst = false;
        if (!named.Success)
        {
            named = DayFirst.Match(text);
            dayFirst = named.Success;
        }

        if (named.Success)
        {
            var monthName = dayFirst ? named.Groups["month"].Value : named.Groups["month"].Value;
            var month = MonthNumber(monthName);
            var yearText = named.Groups["year"].Value;
            var year = yearText.Length > 0 ? int.Parse(yearText, CultureInfo.InvariantCulture) : today.Year;
            var day = int.Parse(named.Groups["day"].Value, CultureInfo.InvariantCulture);
            if (!TryCreateDate(year, month, day, out var checkIn))
                return ParsedDates.Invalid("That date doesn't look valid. Which future check-in date did you mean?");
            var endText = named.Groups["end"].Value;
            DateTime? checkOut = null;
            if (endText.Length > 0)
            {
                var endMonthText = dayFirst && named.Groups["endmonth"].Success
                    ? named.Groups["endmonth"].Value
                    : string.Empty;
                var endMonth = endMonthText.Length > 0 ? MonthNumber(endMonthText) : month;
                var endYear = named.Groups["endyear"].Success
                    ? int.Parse(named.Groups["endyear"].Value, CultureInfo.InvariantCulture)
                    : endMonth < month ? year + 1 : year;
                if (!TryCreateDate(endYear, endMonth, int.Parse(endText, CultureInfo.InvariantCulture), out var parsedOut))
                    return ParsedDates.Invalid("That check-out date doesn't look valid. What date should I use?");
                checkOut = parsedOut;
            }
            return ValidateDates(checkIn, checkOut, today);
        }

        var hasExplicitMonth = requestedMonth.HasValue;
        var hasDateContext = hasExplicitMonth
            || assistantAskedDates
            || Regex.IsMatch(text, @"\b(date|dates|check[\s-]?in|check[\s-]?out|from|on|stay|night|nights)\b", Ci);
        var range = DayRange.Match(text);
        if (range.Success && (hasDateContext || SingleOrdinal.IsMatch(text) || !GuestCount.IsMatch(text)))
        {
            var firstDay = int.Parse(range.Groups["start"].Value, CultureInfo.InvariantCulture);
            var secondDay = int.Parse(range.Groups["end"].Value, CultureInfo.InvariantCulture);
            var month = requestedMonth ?? today.Month;
            var year = requestedYear;
            if (!TryCreateDate(year, month, firstDay, out var checkIn))
                return ParsedDates.Invalid("Those dates don't look valid. What future dates would work?");
            if (!requestedMonth.HasValue && checkIn < today)
            {
                var nextMonth = today.AddMonths(1);
                year = nextMonth.Year;
                month = nextMonth.Month;
                if (!TryCreateDate(year, month, firstDay, out checkIn))
                    return ParsedDates.Invalid("I couldn't match that day to the next month. What future check-in date would work?");
            }
            var checkOutMonth = month;
            var checkOutYear = year;
            if (secondDay <= firstDay)
            {
                var nextMonth = new DateTime(year, month, 1).AddMonths(1);
                checkOutMonth = nextMonth.Month;
                checkOutYear = nextMonth.Year;
            }
            if (!TryCreateDate(checkOutYear, checkOutMonth, secondDay, out var checkOut))
                return ParsedDates.Invalid("That check-out date doesn't look valid. What date should I use?");
            return ValidateDates(checkIn, checkOut, today);
        }

        var dayMatch = SingleOrdinal.Match(text);
        var standaloneDay = SingleDay.Match(text);
        var hasSingleDay = dayMatch.Success || standaloneDay.Success;
        if (hasSingleDay && (hasDateContext || dayMatch.Success || assistantAskedDates))
        {
            var dayText = dayMatch.Success ? dayMatch.Groups["day"].Value : standaloneDay.Groups["day"].Value;
            var day = int.Parse(dayText, CultureInfo.InvariantCulture);
            var month = requestedMonth ?? today.Month;
            var year = requestedYear;
            var targetIsCheckout = assistantAskedCheckout && existingCheckIn.HasValue;
            if (targetIsCheckout)
            {
                month = existingCheckIn!.Value.Month;
                year = existingCheckIn.Value.Year;
                if (!TryCreateDate(year, month, day, out var checkOut))
                {
                    var nextMonth = new DateTime(year, month, 1).AddMonths(1);
                    if (!TryCreateDate(nextMonth.Year, nextMonth.Month, day, out checkOut))
                        return ParsedDates.Invalid("That check-out date doesn't look valid. What date should I use?");
                }
                return ValidateDates(existingCheckIn.Value, checkOut, today);
            }

            if (!TryCreateDate(year, month, day, out var checkIn))
                return ParsedDates.Invalid("That date doesn't look valid. What future check-in date would you like?");
            if (!requestedMonth.HasValue && checkIn < today)
            {
                var nextMonth = today.AddMonths(1);
                if (!TryCreateDate(nextMonth.Year, nextMonth.Month, day, out checkIn))
                    return ParsedDates.Invalid("I couldn't match that day to the next month. What future check-in date would work?");
            }
            return ParsedDates.Valid(checkIn, nightCount.HasValue ? checkIn.AddDays(nightCount.Value) : null);
        }

        if (nightCount.HasValue && existingCheckIn.HasValue && nightCount.Value is > 0 and <= 365)
            return ParsedDates.Valid(null, existingCheckIn.Value.AddDays(nightCount.Value));

        return ParsedDates.None;
    }

    private static ParsedDates ValidateDates(DateTime checkIn, DateTime? checkOut, DateTime today)
    {
        if (checkIn.Date < today.Date)
            return ParsedDates.Invalid("That check-in date has already passed. What future dates would work for your trip?");
        if (checkOut.HasValue && checkOut.Value.Date <= checkIn.Date)
            return ParsedDates.Invalid("Your check-out needs to be after check-in. What dates should I use?");
        return ParsedDates.Valid(checkIn.Date, checkOut?.Date);
    }

    private static bool TryCreateDate(string year, string month, string day, out DateTime result) =>
        TryCreateDate(
            int.Parse(year, CultureInfo.InvariantCulture),
            int.Parse(month, CultureInfo.InvariantCulture),
            int.Parse(day, CultureInfo.InvariantCulture),
            out result);

    private static bool TryCreateDate(int year, int month, int day, out DateTime result)
    {
        try
        {
            result = new DateTime(year, month, day);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            result = default;
            return false;
        }
    }

    private static int MonthNumber(string month) =>
        Array.FindIndex(MonthNames, name => name.StartsWith(month, StringComparison.OrdinalIgnoreCase)) + 1;

    private static (int? Guests, bool Invalid) ParseGuests(string prompt, bool allowBareCount)
    {
        if (Regex.IsMatch(prompt, @"\b(couple|two of us)\b", Ci))
            return (2, false);
        if (Regex.IsMatch(prompt, @"\b(solo|alone|just me|one person)\b", Ci))
            return (1, false);

        var match = GuestCount.Match(prompt);
        string countText = string.Empty;
        if (match.Success)
        {
            countText = match.Groups["count"].Success ? match.Groups["count"].Value
                : match.Groups["family"].Success ? match.Groups["family"].Value
                : match.Groups["for"].Value;
        }
        else if (allowBareCount && SingleDay.Match(prompt) is { Success: true } bare)
        {
            countText = bare.Groups["day"].Value;
        }

        if (countText.Length == 0)
            return (null, false);
        var count = int.Parse(countText, CultureInfo.InvariantCulture);
        return count is >= 1 and <= 50 ? (count, false) : (null, true);
    }

    private sealed record ParsedDates(bool HasDate, DateTime? CheckIn, DateTime? CheckOut, string? Error)
    {
        public static ParsedDates None { get; } = new(false, null, null, null);
        public static ParsedDates Valid(DateTime? checkIn, DateTime? checkOut) => new(true, checkIn, checkOut, null);
        public static ParsedDates Invalid(string error) => new(true, null, null, error);
    }
}
