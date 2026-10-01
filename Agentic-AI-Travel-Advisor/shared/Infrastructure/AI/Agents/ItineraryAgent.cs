using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using TravelAdvisor.Core.DTOs.AI;
using TravelAdvisor.Core.Enums;
using TravelAdvisor.Infrastructure.AI.Planning;
using TravelAdvisor.Infrastructure.AI.Schema;
using TravelAdvisor.Infrastructure.AI.Tools;

namespace TravelAdvisor.Infrastructure.AI.Agents;

/// <summary>Turns a verified selection into the structured plan, the legacy plan and the reply text.</summary>
public class ItineraryAgent
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    private static readonly string[] Slots = ["09:00", "11:30", "14:30", "17:00"];
    private static readonly string[] ArrivalDaySlots = ["15:30", "17:30", "19:00"];

    private readonly ILogger<ItineraryAgent> _logger;

    public ItineraryAgent(ILogger<ItineraryAgent> logger)
    {
        _logger = logger;
    }

    public PlanOutcome Build(TripRequirements req, RecommendationResult rec, AgentToolContext context)
    {
        context.UseAgent(AgentNames.Itinerary);

        if (rec.Selection is null || rec.Destination is null || rec.Input is null)
            return Failure(req, rec);

        var plan = BuildPlan(req, rec);
        var validation = TravelPlanSchema.Validate(plan);
        if (!validation.IsValid)
        {
            _logger.LogError("Generated travel plan failed schema validation: {Errors}", string.Join("; ", validation.Errors));
            return new PlanOutcome
            {
                Status = ChatStatus.Info,
                Requirements = req,
                Message = "I found options for this trip but could not produce a plan that passes validation, so I'm not showing it. Please try again or adjust the request."
            };
        }

        return new PlanOutcome
        {
            Status = ChatStatus.Plan,
            Requirements = req,
            Plan = plan,
            LegacyPlan = ToLegacy(plan),
            Message = BuildReply(plan),
            Recommendations = BuildRecommendations(plan, rec.Selection.Score)
        };
    }

    public static TravelPlan BuildPlan(TripRequirements req, RecommendationResult rec)
    {
        var selection = rec.Selection!;
        var input = rec.Input!;
        var destination = rec.Destination!;
        var start = DateOnly.FromDateTime(req.StartDate!.Value);

        var plan = new TravelPlan
        {
            Destination = destination.Name,
            DestinationId = destination.Id,
            Country = destination.Country,
            Duration = input.Days,
            Nights = input.Nights,
            StartDate = start,
            EndDate = start.AddDays(input.Days - 1),
            Travelers = input.Travelers,
            Budget = input.Budget,
            Currency = "LKR",
            EstimatedTotal = selection.Total,
            WithinBudget = selection.Total <= input.Budget,
            CostBreakdown = new PlanCostBreakdown
            {
                Accommodation = selection.Hotel?.Total ?? 0,
                Packages = selection.Package?.Total ?? 0,
                Transportation = selection.Transport?.Total ?? 0
            },
            Assumptions = [.. req.Assumptions],
            Warnings = [.. req.Warnings.Concat(rec.Warnings).Distinct()]
        };

        if (selection.Hotel is { } hotel)
        {
            plan.Hotels.Add(ToPlanHotel(hotel, input.Nights, selected: true));
            plan.Hotels.AddRange(selection.AlternativeHotels.Select(h => ToPlanHotel(h, input.Nights, selected: false)));
            if (hotel.Rooms > 1)
                plan.Warnings.Add($"Your group needs {hotel.Rooms} rooms of this type; each room is booked separately.");
        }

        if (selection.Package is { } package)
        {
            plan.TravelPackages.Add(ToPlanPackage(package, selected: true, rec.PackageQuote?.RemainingPlaces));
            plan.Assumptions.Add("Package price includes every listed activity for each traveler.");
        }
        plan.TravelPackages.AddRange(selection.AlternativePackages.Select(p => ToPlanPackage(p, selected: false, null)));

        if (selection.Transport is { } transport)
        {
            plan.Transportation.Add(ToPlanTransport(transport, selected: true));
            plan.Assumptions.Add("Transport is priced as a return trip for every traveler.");
        }
        plan.Transportation.AddRange(selection.AlternativeTransport.Select(t => ToPlanTransport(t, selected: false)));

        var freeIdeas = new List<CatalogActivityMatch>();
        if (selection.Package is { } chosen)
        {
            plan.Activities.AddRange(chosen.Package.Activities
                .Where(a => a.DayNumber >= 1 && a.DayNumber <= input.Days)
                .Select(a => new PlanActivity
                {
                    Title = a.Title,
                    Category = a.Category,
                    Day = a.DayNumber,
                    PricePerPerson = a.Price,
                    PackageId = chosen.Package.Id,
                    IncludedInCost = true
                }));
        }
        else
        {
            freeIdeas = rec.DestinationActivities
                .Where(a => a.Price == 0)
                .DistinctBy(a => a.Title, StringComparer.OrdinalIgnoreCase)
                .Take(input.Days * 2)
                .ToList();
            var day = 1;
            foreach (var idea in freeIdeas)
            {
                plan.Activities.Add(new PlanActivity
                {
                    Title = idea.Title,
                    Category = idea.Category,
                    Day = day,
                    PricePerPerson = 0,
                    IncludedInCost = true
                });
                day = day % input.Days + 1;
            }

            var interests = input.Interests;
            plan.Activities.AddRange(rec.DestinationActivities
                .Where(a => a.Price > 0 && interests.Any(i => string.Equals(a.Category, i, StringComparison.OrdinalIgnoreCase)))
                .DistinctBy(a => a.Title, StringComparer.OrdinalIgnoreCase)
                .Take(3)
                .Select(a => new PlanActivity
                {
                    Title = a.Title,
                    Category = a.Category,
                    Day = Math.Min(Math.Max(a.DayNumber, 1), input.Days),
                    PricePerPerson = a.Price,
                    PackageId = a.TravelPackageId,
                    IncludedInCost = false
                }));
            if (plan.Activities.Any(a => !a.IncludedInCost))
                plan.Assumptions.Add("Paid activity ideas are only available as part of their package and are not included in the total.");
        }

        plan.Itinerary = BuildDays(plan, selection, input, destination.Name, start);
        return plan;
    }

    private static List<PlanDay> BuildDays(TravelPlan plan, PlanSelection selection, PlanningInput input, string destinationName, DateOnly start)
    {
        var days = new List<PlanDay>();
        var included = plan.Activities.Where(a => a.IncludedInCost).ToList();

        for (var d = 1; d <= input.Days; d++)
        {
            var items = new List<PlanDayItem>();
            var arrivalDay = d == 1 && (selection.Transport is not null || selection.Hotel is not null);

            if (d == 1 && selection.Transport is { } t)
            {
                items.Add(new PlanDayItem
                {
                    Time = t.Transport.DepartureTime is { } dep ? dep.ToString(@"hh\:mm", Inv) : "07:00",
                    Type = PlanItemTypes.Transport,
                    Title = $"{t.Transport.Mode} from {t.Transport.FromLocation} to {t.Transport.ToLocation}",
                    Description = $"About {Duration(t.Transport.DurationMinutes)}; Rs. {Money(t.Transport.PricePerPerson)} per person each way."
                });
            }

            if (d == 1 && selection.Hotel is { } h)
            {
                items.Add(new PlanDayItem
                {
                    Time = "14:00",
                    Type = PlanItemTypes.CheckIn,
                    Title = $"Check in at {h.Room.HotelName}",
                    Description = $"{h.Room.RoomName}" + (h.Rooms > 1 ? $" × {h.Rooms} rooms" : "")
                });
            }

            var slots = arrivalDay ? ArrivalDaySlots : Slots;
            var dayActivities = included.Where(a => a.Day == d).Take(slots.Length).ToList();

            if (selection.Package is { } pkg && d <= pkg.Package.DurationDays && dayActivities.Count == 0)
            {
                items.Add(new PlanDayItem
                {
                    Time = slots[0],
                    Type = PlanItemTypes.Activity,
                    Title = $"{pkg.Package.Title} (day {d} of {pkg.Package.DurationDays})",
                    Description = "Included in your package; the operator shares the detailed schedule."
                });
            }
            for (var i = 0; i < dayActivities.Count; i++)
            {
                var activity = dayActivities[i];
                items.Add(new PlanDayItem
                {
                    Time = slots[i],
                    Type = PlanItemTypes.Activity,
                    Title = activity.Title,
                    Description = activity.PackageId is not null
                        ? "Included in your package."
                        : "Self-guided, free to visit."
                });
            }

            var lastDay = d == input.Days;
            if (lastDay && selection.Hotel is not null)
            {
                items.Add(new PlanDayItem { Time = "11:00", Type = PlanItemTypes.CheckOut, Title = "Check out of the hotel" });
            }

            if (lastDay && selection.Transport is { } back)
            {
                items.Add(new PlanDayItem
                {
                    Time = "15:00",
                    Type = PlanItemTypes.Transport,
                    Title = $"{back.Transport.Mode} from {back.Transport.ToLocation} back to {back.Transport.FromLocation}",
                    Description = $"Return journey, about {Duration(back.Transport.DurationMinutes)}."
                });
            }

            if (!items.Any(i => i.Type is PlanItemTypes.Activity))
            {
                items.Add(new PlanDayItem
                {
                    Time = arrivalDay ? "16:00" : lastDay ? "09:00" : "10:00",
                    Type = PlanItemTypes.Free,
                    Title = $"Explore {destinationName} at your own pace",
                    Description = "Free time for local food, viewpoints and walks."
                });
            }

            days.Add(new PlanDay
            {
                Day = d,
                Date = start.AddDays(d - 1),
                Items = items.OrderBy(i => i.Time, StringComparer.Ordinal).ToList()
            });
        }

        return days;
    }

    public static string BuildReply(TravelPlan plan)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Here is a {plan.Duration}-day plan for {plan.Destination} for {plan.Travelers} traveler(s), {plan.StartDate:yyyy-MM-dd} to {plan.EndDate:yyyy-MM-dd}.");
        sb.AppendLine($"Estimated total: Rs. {Money(plan.EstimatedTotal)} of your Rs. {Money(plan.Budget)} budget (Rs. {Money(plan.Budget - plan.EstimatedTotal)} to spare).");
        sb.AppendLine();

        var hotel = plan.Hotels.FirstOrDefault(h => h.Selected);
        if (hotel is not null)
            sb.AppendLine($"Stay: {hotel.Name} – {hotel.RoomName}, Rs. {Money(hotel.PricePerNight)}/night × {hotel.Nights} night(s)" +
                          (hotel.Rooms > 1 ? $" × {hotel.Rooms} rooms" : "") + $" = Rs. {Money(hotel.TotalCost)} (available for your dates).");

        var package = plan.TravelPackages.FirstOrDefault(p => p.Selected);
        if (package is not null)
            sb.AppendLine($"Package: {package.Title}, Rs. {Money(package.PricePerPerson)} per person incl. activities = Rs. {Money(package.TotalCost)}.");
        else
            sb.AppendLine("Package: none included – no package fits this trip and budget together.");

        var transport = plan.Transportation.FirstOrDefault(t => t.Selected);
        if (transport is not null)
            sb.AppendLine($"Transport: {transport.Mode} {transport.From} → {transport.To}, return for {plan.Travelers} = Rs. {Money(transport.TotalCost)}.");

        sb.AppendLine();
        foreach (var day in plan.Itinerary)
        {
            sb.AppendLine($"Day {day.Day} ({day.Date:yyyy-MM-dd}):");
            foreach (var item in day.Items)
                sb.AppendLine($"  • {item.Time} {item.Title}");
        }

        var alternatives = plan.Hotels.Where(h => !h.Selected).Select(h => $"{h.Name} ({h.RoomName}, Rs. {Money(h.TotalCost)})")
            .Concat(plan.TravelPackages.Where(p => !p.Selected).Select(p => $"{p.Title} package (Rs. {Money(p.TotalCost)})"))
            .ToList();
        if (alternatives.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Other options that also fit the budget: " + string.Join("; ", alternatives) + ".");
        }

        if (plan.Assumptions.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Assumptions: " + string.Join(" ", plan.Assumptions));
        }

        if (plan.Warnings.Count > 0)
            sb.AppendLine("Notes: " + string.Join(" ", plan.Warnings));

        sb.AppendLine();
        sb.Append("Nothing has been booked yet. Say \"book the hotel\"" + (package is not null ? " or \"book the package\"" : "") +
                  " and I'll check availability and ask you to confirm, or tap Save itinerary to keep this plan.");
        return sb.ToString();
    }

    public static SuggestedTravelPlan ToLegacy(TravelPlan plan)
    {
        var hotel = plan.Hotels.FirstOrDefault(h => h.Selected);
        var package = plan.TravelPackages.FirstOrDefault(p => p.Selected);
        var summary = new List<string>();
        if (hotel is not null)
            summary.Add($"{hotel.Name} ({hotel.RoomName}, Rs. {Money(hotel.PricePerNight)}/night × {hotel.Nights} nights)");
        if (package is not null)
            summary.Add($"{package.Title} package (Rs. {Money(package.TotalCost)} for {plan.Travelers})");

        return new SuggestedTravelPlan
        {
            Title = $"{plan.Duration}-Day {plan.Destination} Trip",
            DestinationId = plan.DestinationId,
            DestinationName = plan.Destination,
            StartDate = DateTime.SpecifyKind(plan.StartDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc),
            EndDate = DateTime.SpecifyKind(plan.EndDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc),
            Travelers = plan.Travelers,
            Budget = plan.Budget,
            EstimatedCost = plan.EstimatedTotal,
            Summary = string.Join(". ", summary),
            Hotel = hotel is null
                ? null
                : new SuggestedHotel
                {
                    Id = hotel.HotelId,
                    Name = hotel.Name,
                    City = hotel.City,
                    RoomId = hotel.RoomId,
                    RoomName = hotel.RoomName,
                    PricePerNight = hotel.PricePerNight
                },
            Package = package is null
                ? null
                : new SuggestedPackage
                {
                    Id = package.PackageId,
                    Title = package.Title,
                    Price = package.TotalCost,
                    DurationDays = package.DurationDays
                },
            Items = plan.Itinerary
                .SelectMany(day => day.Items.Select((item, index) => new SuggestedPlanItem
                {
                    DayNumber = day.Day,
                    Title = item.Title,
                    Description = item.Description,
                    StartTime = TimeSpan.TryParse(item.Time, Inv, out var time) ? time : null,
                    SortOrder = index
                }))
                .ToList()
        };
    }

    private static List<RecommendationRecord> BuildRecommendations(TravelPlan plan, double score)
    {
        var records = new List<RecommendationRecord>();
        var rounded = Math.Round(score, 3);

        foreach (var h in plan.Hotels)
            records.Add(new RecommendationRecord(RecommendationType.Hotel, h.HotelId, h.RoomId, null, null, plan.DestinationId,
                Clip($"{h.Name} – {h.RoomName}"), h.TotalCost, h.Selected ? rounded : Math.Round(rounded * 0.8, 3),
                h.Selected ? "Selected: best fit for the stay preference within budget; dates checked live." : "Alternative that keeps the plan within budget."));

        foreach (var p in plan.TravelPackages)
            records.Add(new RecommendationRecord(RecommendationType.Package, null, null, p.PackageId, null, plan.DestinationId,
                Clip(p.Title), p.TotalCost, p.Selected ? rounded : Math.Round(rounded * 0.8, 3),
                p.Selected ? "Selected: matches interests and fits the budget for every traveler." : "Alternative package that keeps the plan within budget."));

        foreach (var t in plan.Transportation)
            records.Add(new RecommendationRecord(RecommendationType.Transportation, null, null, null, t.TransportationId, plan.DestinationId,
                Clip($"{t.Mode} {t.From} → {t.To}"), t.TotalCost, t.Selected ? rounded : Math.Round(rounded * 0.8, 3),
                t.Selected ? "Selected: return trip for every traveler within budget." : "Alternative transport within budget."));

        foreach (var a in plan.Activities.Where(a => a.IncludedInCost))
            records.Add(new RecommendationRecord(RecommendationType.Activity, null, null, a.PackageId, null, plan.DestinationId,
                Clip(a.Title), a.PricePerPerson * plan.Travelers, rounded, $"Day {a.Day} activity in the plan."));

        return records;
    }

    private static PlanOutcome Failure(TripRequirements req, RecommendationResult rec)
    {
        var travelers = rec.Input?.Travelers ?? req.Travelers;
        var days = rec.Input?.Days ?? req.DurationDays ?? RequirementParser.DefaultDurationDays;
        string message;

        if (rec.Status == ChatStatus.OverBudget && rec.CheapestTotal is { } cheapest)
        {
            message = $"I couldn't fit a {days}-day {rec.Destination?.Name ?? req.Destination} trip for {travelers} traveler(s) into Rs. {Money(req.Budget ?? 0)}. " +
                      $"The cheapest option I found costs Rs. {Money(cheapest)}" +
                      (rec.CheapestDescription is { } d ? $" ({d})" : "") +
                      ". You could raise the budget, shorten the trip or travel with fewer people, and I'll try again.";
        }
        else
        {
            message = rec.Error ?? "I couldn't find options for that trip.";
        }

        if (rec.Warnings.Count > 0)
            message += " " + string.Join(" ", rec.Warnings.Distinct());

        return new PlanOutcome
        {
            Status = rec.Status == ChatStatus.Plan ? ChatStatus.NoMatch : rec.Status,
            Requirements = req,
            Message = message
        };
    }

    private static PlanHotel ToPlanHotel(HotelOption option, int nights, bool selected) => new()
    {
        HotelId = option.Room.HotelId,
        RoomId = option.Room.RoomId,
        Name = option.Room.HotelName,
        RoomName = option.Room.RoomName,
        City = option.Room.City,
        PricePerNight = option.Room.PricePerNight,
        Nights = nights,
        Rooms = option.Rooms,
        Capacity = option.Room.Capacity,
        TotalCost = option.Total,
        AverageRating = option.Room.AverageRating,
        Selected = selected,
        AvailabilityChecked = selected
    };

    private static PlanPackage ToPlanPackage(PackageOption option, bool selected, int? remainingPlaces) => new()
    {
        PackageId = option.Package.Id,
        Title = option.Package.Title,
        DurationDays = option.Package.DurationDays,
        PricePerPerson = option.Package.PricePerPerson,
        TotalCost = option.Total,
        RemainingPlaces = remainingPlaces,
        AverageRating = option.Package.AverageRating,
        Selected = selected,
        AvailabilityChecked = selected
    };

    private static PlanTransport ToPlanTransport(TransportOption option, bool selected) => new()
    {
        TransportationId = option.Transport.Id,
        Mode = option.Transport.Mode.ToString(),
        From = option.Transport.FromLocation,
        To = option.Transport.ToLocation,
        DepartureTime = option.Transport.DepartureTime?.ToString(@"hh\:mm", Inv),
        DurationMinutes = option.Transport.DurationMinutes,
        PricePerPerson = option.Transport.PricePerPerson,
        Trips = option.Trips,
        TotalCost = option.Total,
        Selected = selected
    };

    private static string Money(decimal value) => value.ToString("N0", Inv);

    private static string Duration(int minutes) =>
        minutes >= 60 ? $"{minutes / 60}h{(minutes % 60 > 0 ? $" {minutes % 60}m" : "")}" : $"{minutes}m";

    private static string Clip(string text) => text.Length <= 200 ? text : text[..197] + "...";
}
