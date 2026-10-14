using TravelAdvisor.Infrastructure.AI.Planning;
using Xunit;

namespace TravelAdvisor.UnitTests.AI;

public class BudgetPlannerTests
{
    private static PlanningInput Input(decimal budget, int days = 3, int travelers = 2, string stay = "mid-range", string? transport = null) =>
        new(days, travelers, budget, stay, ["hiking"], transport);

    private static BudgetPlanResult Plan(PlanningInput input) =>
        BudgetPlanner.Select(
            input,
            BudgetPlanner.BuildHotelOptions(input, AiTestData.EllaRooms()),
            BudgetPlanner.BuildPackageOptions(input, AiTestData.EllaPackages()),
            BudgetPlanner.BuildTransportOptions(input, AiTestData.EllaTransport(), "Ella"));

    [Fact]
    public void Package_total_includes_activities_for_every_traveler()
    {
        var option = Assert.Single(BudgetPlanner.BuildPackageOptions(Input(100000), AiTestData.EllaPackages()));
        Assert.Equal((28000m + 0m + 8000m) * 2, option.Total);
    }

    [Fact]
    public void Def007_package_that_would_break_the_budget_is_not_selected()
    {
        var result = Plan(Input(50000));

        Assert.NotNull(result.Selection);
        Assert.Null(result.Selection!.Package);
        Assert.NotNull(result.Selection.Hotel);
        Assert.True(result.Selection.Total <= 50000m, $"total {result.Selection.Total}");
    }

    [Theory]
    [InlineData(10000)]
    [InlineData(16000)]
    [InlineData(23999)]
    [InlineData(24000)]
    [InlineData(50000)]
    [InlineData(88000)]
    [InlineData(100000)]
    [InlineData(250000)]
    public void Selection_and_alternatives_never_exceed_the_budget(decimal budget)
    {
        foreach (var travelers in new[] { 1, 2, 3, 5 })
        foreach (var days in new[] { 1, 2, 3, 5 })
        {
            var result = Plan(Input(budget, days, travelers));
            if (result.Selection is not { } s)
                continue;

            Assert.True(s.Total <= budget, $"budget {budget}, {travelers} travelers, {days} days: total {s.Total}");
            foreach (var alt in s.AlternativeHotels)
                Assert.True(s.Total - (s.Hotel?.Total ?? 0) + alt.Total <= budget);
            foreach (var alt in s.AlternativePackages)
                Assert.True(s.Total - (s.Package?.Total ?? 0) + alt.Total <= budget);
            foreach (var alt in s.AlternativeTransport)
                Assert.True(s.Total - (s.Transport?.Total ?? 0) + alt.Total <= budget);
        }
    }

    [Fact]
    public void Too_small_budget_reports_the_cheapest_real_option()
    {
        var result = Plan(Input(10000));

        Assert.Null(result.Selection);
        Assert.Equal(16000m, result.CheapestTotal);
        Assert.Contains("Garden Double", result.CheapestDescription);
        Assert.Contains("16,000", result.FailureReason);
    }

    [Fact]
    public void Rooms_are_multiplied_for_larger_groups()
    {
        var options = BudgetPlanner.BuildHotelOptions(Input(500000, travelers: 5), AiTestData.EllaRooms());

        var family = options.Single(o => o.Room.RoomId == 6);
        Assert.Equal(2, family.Rooms);
        Assert.Equal(12000m * 2 * 2, family.Total);
        Assert.Equal(3, options.Single(o => o.Room.RoomId == 5).Rooms);
    }

    [Fact]
    public void Package_longer_than_trip_or_too_small_for_group_is_excluded()
    {
        Assert.Empty(BudgetPlanner.BuildPackageOptions(Input(500000, days: 1), AiTestData.EllaPackages()));
        Assert.Empty(BudgetPlanner.BuildPackageOptions(Input(500000, travelers: 11), AiTestData.EllaPackages()));
    }

    [Fact]
    public void Local_routes_inside_the_destination_are_not_offered_as_transport_there()
    {
        var option = Assert.Single(BudgetPlanner.BuildTransportOptions(Input(100000), AiTestData.EllaTransport(), "Ella"));
        Assert.Equal("Kandy", option.Transport.FromLocation);
        Assert.Equal(2000m * 2 * 2, option.Total);
    }

    [Fact]
    public void Day_trip_needs_no_hotel()
    {
        var result = Plan(Input(20000, days: 1));

        Assert.NotNull(result.Selection);
        Assert.Null(result.Selection!.Hotel);
        Assert.Equal(8000m, result.Selection.Total);
    }

    [Fact]
    public void Generous_budget_selects_hotel_package_and_transport()
    {
        var result = Plan(Input(150000, transport: "Train"));

        var s = Assert.IsType<PlanSelection>(result.Selection);
        Assert.NotNull(s.Hotel);
        Assert.NotNull(s.Package);
        Assert.NotNull(s.Transport);
        Assert.True(s.Total <= 150000m);
    }

    [Fact]
    public void Budget_preference_prefers_the_cheaper_room()
    {
        var result = Plan(Input(40000, stay: "budget"));
        Assert.Equal(5, result.Selection!.Hotel!.Room.RoomId);
    }
}
