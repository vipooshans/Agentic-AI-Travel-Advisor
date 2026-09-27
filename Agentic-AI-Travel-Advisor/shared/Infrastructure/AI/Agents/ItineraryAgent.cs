using TravelAdvisor.Core.DTOs.AI;

namespace TravelAdvisor.Infrastructure.AI.Agents;

public class ItineraryAgent
{
    public SuggestedTravelPlan Build(TripRequirements requirements, RecommendationResult rec)
    {
        var destination = rec.Destination!;
        var start = requirements.StartDate!.Value;
        var end = requirements.EndDate!.Value;
        var duration = requirements.DurationDays ?? 3;
        var nights = rec.Nights;

        decimal hotelCost = rec.Hotel is null ? 0 : rec.Hotel.PricePerNight * nights;
        decimal packageCost = rec.Package?.Price ?? 0;
        var estimated = hotelCost + packageCost;

        var items = BuildItems(duration, rec);

        var summaryParts = new List<string>();
        if (rec.Hotel is not null)
            summaryParts.Add($"{rec.Hotel.HotelName} ({rec.Hotel.RoomName}, Rs. {rec.Hotel.PricePerNight:0}/night × {nights} nights)");
        if (rec.Package is not null)
            summaryParts.Add($"{rec.Package.Title} package (Rs. {rec.Package.Price:0})");

        return new SuggestedTravelPlan
        {
            Title = $"{duration}-Day {destination.Name} Trip",
            DestinationId = destination.Id,
            DestinationName = destination.Name,
            StartDate = start,
            EndDate = end,
            Travelers = requirements.Travelers,
            Budget = requirements.Budget ?? 0,
            EstimatedCost = estimated,
            Summary = string.Join(". ", summaryParts),
            Hotel = rec.Hotel is null
                ? null
                : new SuggestedHotel
                {
                    Id = rec.Hotel.HotelId,
                    Name = rec.Hotel.HotelName,
                    City = rec.Hotel.City,
                    RoomId = rec.Hotel.RoomId,
                    RoomName = rec.Hotel.RoomName,
                    PricePerNight = rec.Hotel.PricePerNight
                },
            Package = rec.Package is null
                ? null
                : new SuggestedPackage
                {
                    Id = rec.Package.Id,
                    Title = rec.Package.Title,
                    Price = rec.Package.Price,
                    DurationDays = rec.Package.DurationDays
                },
            Items = items
        };
    }

    public string BuildReply(TripRequirements requirements, SuggestedTravelPlan plan)
    {
        var overBudget = plan.EstimatedCost > plan.Budget && plan.Budget > 0;
        var lines = new List<string>
        {
            $"Here is a {requirements.DurationDays}-day plan for {plan.DestinationName} under Rs. {plan.Budget:0}.",
            "",
            $"Assumptions: {plan.Travelers} traveler(s), {requirements.Interests}, {requirements.AccommodationPreference} stay, {plan.StartDate:yyyy-MM-dd} to {plan.EndDate:yyyy-MM-dd}.",
            ""
        };

        if (plan.Hotel is not null)
            lines.Add($"Hotel: {plan.Hotel.Name} — {plan.Hotel.RoomName} at Rs. {plan.Hotel.PricePerNight:0}/night.");
        if (plan.Package is not null)
            lines.Add($"Package: {plan.Package.Title} (Rs. {plan.Package.Price:0}).");

        lines.Add($"Estimated total: Rs. {plan.EstimatedCost:0}" +
                  (overBudget ? $" (slightly over your Rs. {plan.Budget:0} budget)." : $" (within your Rs. {plan.Budget:0} budget)."));
        lines.Add("");

        foreach (var group in plan.Items.GroupBy(i => i.DayNumber).OrderBy(g => g.Key))
        {
            lines.Add($"Day {group.Key}:");
            foreach (var item in group.OrderBy(i => i.SortOrder))
            {
                var time = item.StartTime.HasValue ? item.StartTime.Value.ToString(@"hh\:mm") + " — " : "";
                lines.Add($"  • {time}{item.Title}" + (string.IsNullOrWhiteSpace(item.Description) ? "" : $" ({item.Description})"));
            }
        }

        lines.Add("");
        lines.Add("Tap Save itinerary if you want to keep this plan.");
        return string.Join("\n", lines);
    }

    private static List<SuggestedPlanItem> BuildItems(int durationDays, RecommendationResult rec)
    {
        var items = new List<SuggestedPlanItem>();
        var sort = 0;

        if (rec.Package?.Activities.Count > 0)
        {
            foreach (var activity in rec.Package.Activities.Where(a => a.DayNumber <= durationDays))
            {
                items.Add(new SuggestedPlanItem
                {
                    DayNumber = activity.DayNumber,
                    Title = activity.Title,
                    Description = activity.Description,
                    StartTime = null,
                    SortOrder = sort++
                });
            }
        }

        for (var day = 1; day <= durationDays; day++)
        {
            if (items.Any(i => i.DayNumber == day))
                continue;

            if (day == 1 && rec.Hotel is not null)
            {
                items.Add(new SuggestedPlanItem
                {
                    DayNumber = day,
                    Title = $"Check in at {rec.Hotel.HotelName}",
                    Description = rec.Hotel.Description ?? "Settle in and explore the area.",
                    StartTime = new TimeSpan(14, 0, 0),
                    SortOrder = sort++
                });
            }
            else
            {
                items.Add(new SuggestedPlanItem
                {
                    DayNumber = day,
                    Title = $"Explore {rec.Destination?.Name}",
                    Description = "Free time for local food, viewpoints, and walks.",
                    StartTime = new TimeSpan(9, 30, 0),
                    SortOrder = sort++
                });
            }
        }

        return items.OrderBy(i => i.DayNumber).ThenBy(i => i.SortOrder).Select((item, index) =>
        {
            item.SortOrder = index;
            return item;
        }).GroupBy(i => i.DayNumber).SelectMany(group =>
        {
            var ordered = group.OrderBy(i => i.SortOrder).ToList();
            for (var i = 0; i < ordered.Count; i++)
                ordered[i].StartTime ??= DefaultTime(i + 1);
            return ordered;
        }).ToList();
    }

    private static TimeSpan DefaultTime(int sortOrder) => sortOrder switch
    {
        <= 1 => new TimeSpan(9, 0, 0),
        2 => new TimeSpan(13, 30, 0),
        _ => new TimeSpan(16, 0, 0)
    };
}
