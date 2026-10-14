using System.Text.Json;
using System.Text.Json.Nodes;
using TravelAdvisor.Core.DTOs.AI;
using TravelAdvisor.Infrastructure.AI.Agents;
using TravelAdvisor.Infrastructure.AI.Planning;
using TravelAdvisor.Infrastructure.AI.Schema;
using Xunit;

namespace TravelAdvisor.UnitTests.AI;

public class TravelPlanSchemaTests
{
    private static TravelPlan BuildPlan(decimal budget, int days = 3, int travelers = 2)
    {
        var input = new PlanningInput(days, travelers, budget, "mid-range", ["hiking"], "Train");
        var selection = BudgetPlanner.Select(
            input,
            BudgetPlanner.BuildHotelOptions(input, AiTestData.EllaRooms()),
            BudgetPlanner.BuildPackageOptions(input, AiTestData.EllaPackages()),
            BudgetPlanner.BuildTransportOptions(input, AiTestData.EllaTransport(), "Ella")).Selection;
        Assert.NotNull(selection);

        var req = new TripRequirements
        {
            Destination = "Ella",
            Budget = budget,
            Travelers = travelers,
            DurationDays = days,
            StartDate = new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc)
        };
        var rec = new RecommendationResult
        {
            Destination = AiTestData.Ella,
            Input = input,
            Selection = selection,
            DestinationActivities = AiTestData.EllaActivities()
        };
        return ItineraryAgent.BuildPlan(req, rec);
    }

    [Theory]
    [InlineData(30000, 3, 2)]
    [InlineData(50000, 3, 2)]
    [InlineData(150000, 3, 2)]
    [InlineData(400000, 5, 4)]
    [InlineData(20000, 1, 2)]
    public void Plans_built_from_catalog_data_pass_schema_and_business_rules(decimal budget, int days, int travelers)
    {
        var plan = BuildPlan(budget, days, travelers);
        var result = TravelPlanSchema.Validate(plan);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Equal(days, plan.Itinerary.Count);
        Assert.True(plan.EstimatedTotal <= budget);
    }

    [Fact]
    public void Plan_with_package_lists_it_in_the_itinerary()
    {
        var plan = BuildPlan(150000);

        Assert.Contains(plan.TravelPackages, p => p.Selected);
        Assert.Contains(plan.Itinerary.SelectMany(d => d.Items), i => i.Type == PlanItemTypes.Activity);
    }

    [Fact]
    public void Over_budget_plan_is_rejected()
    {
        var plan = BuildPlan(50000);
        plan.Budget = plan.EstimatedTotal - 1;

        var result = TravelPlanSchema.Validate(plan);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("exceeds budget"));
    }

    [Fact]
    public void Within_budget_flag_must_be_true()
    {
        var plan = BuildPlan(50000);
        plan.WithinBudget = false;
        Assert.False(TravelPlanSchema.Validate(plan).IsValid);
    }

    [Fact]
    public void Currency_must_be_lkr()
    {
        var plan = BuildPlan(50000);
        plan.Currency = "USD";
        Assert.False(TravelPlanSchema.Validate(plan).IsValid);
    }

    [Fact]
    public void Selected_items_must_add_up_to_the_total()
    {
        var plan = BuildPlan(50000);
        plan.EstimatedTotal -= 5000;
        plan.CostBreakdown.Accommodation -= 5000;

        var result = TravelPlanSchema.Validate(plan);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Selected items cost"));
    }

    [Fact]
    public void Selected_hotel_without_availability_check_is_rejected()
    {
        var plan = BuildPlan(50000);
        plan.Hotels.First(h => h.Selected).AvailabilityChecked = false;

        Assert.Contains(TravelPlanSchema.Validate(plan).Errors, e => e.Contains("checkAvailability"));
    }

    [Fact]
    public void Two_selected_hotels_are_rejected()
    {
        var plan = BuildPlan(150000);
        Assert.True(plan.Hotels.Count > 1);
        foreach (var hotel in plan.Hotels)
        {
            hotel.Selected = true;
            hotel.AvailabilityChecked = true;
        }

        Assert.Contains(TravelPlanSchema.Validate(plan).Errors, e => e.Contains("At most one"));
    }

    [Fact]
    public void Missing_itinerary_day_is_rejected()
    {
        var plan = BuildPlan(50000);
        plan.Itinerary.RemoveAt(1);

        Assert.Contains(TravelPlanSchema.Validate(plan).Errors, e => e.Contains("one entry per trip day"));
    }

    [Fact]
    public void Malformed_time_is_rejected_by_the_schema()
    {
        var plan = BuildPlan(50000);
        plan.Itinerary[0].Items[0].Time = "2pm";
        Assert.False(TravelPlanSchema.Validate(plan).IsValid);
    }

    [Fact]
    public void Unknown_properties_are_rejected()
    {
        var node = JsonSerializer.SerializeToNode(BuildPlan(50000), TravelPlanSchema.JsonOptions)!.AsObject();
        node["internalNotes"] = "db password is ...";

        using var doc = JsonDocument.Parse(node.ToJsonString());
        Assert.False(TravelPlanSchema.Validate(doc.RootElement).IsValid);
    }

    [Fact]
    public void Required_fields_are_enforced()
    {
        var node = JsonSerializer.SerializeToNode(BuildPlan(50000), TravelPlanSchema.JsonOptions)!.AsObject();
        node.Remove("itinerary");

        using var doc = JsonDocument.Parse(node.ToJsonString());
        Assert.False(TravelPlanSchema.Validate(doc.RootElement).IsValid);
    }
}
