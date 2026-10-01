using TravelAdvisor.Core.DTOs.AI;
using TravelAdvisor.Core.Enums;
using TravelAdvisor.Infrastructure.AI.Planning;
using Xunit;

namespace TravelAdvisor.UnitTests.AI;

public class RequirementParserTests
{
    private static readonly DateOnly Today = new(2026, 10, 1);
    private static readonly string[] Destinations = ["Ella", "Kandy", "Galle", "Nuwara Eliya"];
    private static readonly Dictionary<string, decimal> Rates = new(StringComparer.OrdinalIgnoreCase) { ["USD"] = 300m };

    private static TripRequirements Parse(params string[] userMessages) =>
        RequirementParser.Parse(userMessages.Select(m => new ChatMessageDto { Role = "user", Content = m }).ToList(), Destinations, Today, Rates);

    [Fact]
    public void Def008_people_count_is_not_read_as_budget()
    {
        var req = Parse("I want a trip with max 4 people, budget Rs. 60000 to Kandy");

        Assert.Equal(60000m, req.Budget);
        Assert.Equal(4, req.Travelers);
        Assert.Equal("Kandy", req.Destination);
    }

    [Theory]
    [InlineData("Plan 3 days in Ella for 2 people from Kandy by train, budget Rs. 60,000", "Ella", "Kandy")]
    [InlineData("From Kandy to Ella for 3 days, budget 60000", "Ella", "Kandy")]
    [InlineData("A trip to Ella from Nuwara Eliya, budget 60000", "Ella", "Nuwara Eliya")]
    [InlineData("Kandy for 2 nights, budget 40000", "Kandy", null)]
    public void Def019_origin_city_is_not_taken_as_the_destination(string message, string destination, string? origin)
    {
        var req = Parse(message);

        Assert.Equal(destination, req.Destination);
        Assert.Equal(origin, req.Origin);
    }

    [Fact]
    public void Def019_only_an_origin_leaves_the_destination_missing()
    {
        Assert.Null(Parse("A trip from Kandy, budget 60000").Destination);
    }

    [Theory]
    [InlineData("budget 50k", 50000)]
    [InlineData("around LKR 75,000 in total", 75000)]
    [InlineData("we can spend 1.5 lakhs", 150000)]
    [InlineData("Rs.45000", 45000)]
    [InlineData("50000", 50000)]
    [InlineData("3 days in Ella under 40000 rupees", 40000)]
    public void Budget_formats(string message, decimal expected)
    {
        Assert.Equal(expected, RequirementParser.ParseBudget(message)?.Amount);
    }

    [Theory]
    [InlineData("3 days for 2 people")]
    [InlineData("in 2026-11-10 for 4 travellers")]
    [InlineData("a 5 star hotel for 3 nights")]
    public void Numbers_that_are_not_money_are_ignored(string message)
    {
        Assert.Null(RequirementParser.ParseBudget(message));
    }

    [Fact]
    public void Foreign_currency_is_converted_with_a_stated_assumption()
    {
        var req = Parse("Ella for 2 people, budget $500");

        Assert.Equal(150000m, req.Budget);
        Assert.Equal("USD", req.OriginalCurrency);
        Assert.Equal(500m, req.OriginalBudget);
        Assert.Contains(req.Assumptions, a => a.Contains("300 LKR per USD"));
    }

    [Fact]
    public void Unknown_currency_rate_leaves_budget_missing_with_a_warning()
    {
        var req = RequirementParser.Parse([new ChatMessageDto { Role = "user", Content = "Ella, budget 400 GBP" }], Destinations, Today, Rates);

        Assert.Null(req.Budget);
        Assert.Contains(req.Warnings, w => w.Contains("GBP"));
    }

    [Theory]
    [InlineData("4 travellers", 4)]
    [InlineData("4 travelers", 4)]
    [InlineData("three adults", 3)]
    [InlineData("a family of 5", 5)]
    [InlineData("honeymoon trip", 2)]
    [InlineData("going solo", 1)]
    [InlineData("me with 3 friends", 4)]
    [InlineData("Ella for 6", 6)]
    public void Travelers(string message, int expected)
    {
        Assert.Equal(expected, RequirementParser.ParseTravelers(message));
    }

    [Theory]
    [InlineData("a 4-day trip", 4)]
    [InlineData("3 nights", 4)]
    [InlineData("one week", 7)]
    [InlineData("this weekend", 2)]
    public void Duration(string message, int expected)
    {
        Assert.Equal(expected, RequirementParser.ParseDurationDays(message));
    }

    [Fact]
    public void Date_range_sets_start_and_duration()
    {
        var req = Parse("Galle from 10 Nov to 13 Nov, budget 80000");

        Assert.Equal(new DateTime(2026, 11, 10, 0, 0, 0, DateTimeKind.Utc), req.StartDate);
        Assert.Equal(4, req.DurationDays);
    }

    [Fact]
    public void Past_date_is_rejected_with_a_warning()
    {
        var warnings = new List<string>();
        Assert.Null(RequirementParser.ParseDates("on 2026-09-01", Today, warnings));
        Assert.Single(warnings);
    }

    [Fact]
    public void Month_without_year_rolls_to_next_year_when_already_past()
    {
        Assert.Equal(new DateOnly(2027, 3, 5), RequirementParser.ParseDates("March 5", Today)?.Start);
    }

    [Fact]
    public void Latest_message_wins_but_earlier_fields_are_kept()
    {
        var req = Parse("Plan Ella for 2 people, budget 50000", "make it 4 people");

        Assert.Equal("Ella", req.Destination);
        Assert.Equal(50000m, req.Budget);
        Assert.Equal(4, req.Travelers);
    }

    [Fact]
    public void Transport_origin_accommodation_and_interests()
    {
        var req = Parse("Cheap stay in Ella from Colombo by train, we love hiking and tea, budget 60000");

        Assert.Equal(nameof(TransportMode.Train), req.TransportPreference);
        Assert.Equal("Colombo", req.Origin);
        Assert.Equal("budget", req.AccommodationPreference);
        Assert.Contains("hiking", req.Interests);
        Assert.Contains("nature", req.Interests);
    }

    [Fact]
    public void Defaults_are_applied_and_recorded_as_assumptions()
    {
        var req = Parse("Ella, budget 50000");
        RequirementParser.ApplyDefaults(req, Today);

        Assert.Equal(2, req.Travelers);
        Assert.Equal(3, req.DurationDays);
        Assert.Equal(new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc), req.StartDate);
        Assert.Contains(req.Assumptions, a => a.Contains("2 travelers"));
        Assert.Contains(req.Assumptions, a => a.Contains("3-day trip"));
        Assert.Contains(req.Assumptions, a => a.Contains("2026-10-15"));
        Assert.Empty(req.MissingFields);
    }

    [Fact]
    public void Missing_destination_and_budget_are_reported()
    {
        var req = Parse("I want a holiday");
        RequirementParser.ApplyDefaults(req, Today);

        Assert.Contains("destination", req.MissingFields);
        Assert.Contains("budget", req.MissingFields);
        Assert.False(req.HasEnoughToPlan);
    }

    [Fact]
    public void Group_size_and_duration_are_capped()
    {
        var req = new TripRequirements { Destination = "Ella", Budget = 1_000_000, Travelers = 45, DurationDays = 90 };
        RequirementParser.ApplyDefaults(req, Today);

        Assert.Equal(RequirementParser.MaxTravelers, req.Travelers);
        Assert.Equal(RequirementParser.MaxDurationDays, req.DurationDays);
        Assert.Equal(2, req.Warnings.Count);
    }
}
