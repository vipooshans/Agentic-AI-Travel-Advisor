using TravelAdvisor.Core.DTOs.AI;

namespace TravelAdvisor.Infrastructure.AI.Planning;

public sealed record PlanningInput(
    int Days,
    int Travelers,
    decimal Budget,
    string AccommodationPreference,
    IReadOnlyList<string> Interests,
    string? TransportPreference)
{
    /// <summary>A one-day trip needs no hotel night.</summary>
    public int Nights => Math.Max(0, Days - 1);
}

public sealed class HotelOption
{
    public required CatalogHotelMatch Room { get; init; }
    public int Rooms { get; init; } = 1;
    public decimal Total { get; set; }
    public double Fit { get; set; }
}

public sealed class PackageOption
{
    public required CatalogPackageMatch Package { get; init; }
    public decimal Total { get; init; }
    public double InterestMatch { get; init; }
}

public sealed class TransportOption
{
    public required CatalogTransportMatch Transport { get; init; }
    public int Trips { get; init; } = 2;
    public decimal Total { get; init; }
}

public sealed class PlanSelection
{
    public HotelOption? Hotel { get; init; }
    public PackageOption? Package { get; init; }
    public TransportOption? Transport { get; init; }
    public double Score { get; init; }
    public decimal Total => (Hotel?.Total ?? 0) + (Package?.Total ?? 0) + (Transport?.Total ?? 0);
    public List<HotelOption> AlternativeHotels { get; } = [];
    public List<PackageOption> AlternativePackages { get; } = [];
    public List<TransportOption> AlternativeTransport { get; } = [];
}

public sealed class BudgetPlanResult
{
    public PlanSelection? Selection { get; init; }

    /// <summary>Cheapest bookable combination when nothing fits the budget.</summary>
    public decimal? CheapestTotal { get; init; }
    public string? CheapestDescription { get; init; }
    public string? FailureReason { get; init; }
}

/// <summary>
/// Chooses the best hotel/package/transport combination whose full cost (activities and every
/// traveler included) stays within the budget. It never returns an over-budget selection (DEF-007).
/// </summary>
public static class BudgetPlanner
{
    public const int MaxRoomsPerPlan = 4;
    private const int MaxHotelCandidates = 20;
    private const int MaxPackageCandidates = 10;
    private const int MaxTransportCandidates = 5;

    public static List<HotelOption> BuildHotelOptions(PlanningInput input, IEnumerable<CatalogHotelMatch> rooms, ISet<int>? excludedRooms = null)
    {
        if (input.Nights == 0)
            return [];

        var options = rooms
            .Where(r => r.PricePerNight > 0 && r.Capacity > 0 && excludedRooms?.Contains(r.RoomId) != true)
            .Select(r =>
            {
                var roomsNeeded = (int)Math.Ceiling(input.Travelers / (double)r.Capacity);
                return new HotelOption
                {
                    Room = r,
                    Rooms = roomsNeeded,
                    Total = r.PricePerNight * input.Nights * roomsNeeded
                };
            })
            .Where(o => o.Rooms <= MaxRoomsPerPlan)
            .OrderBy(o => o.Rooms)
            .ThenBy(o => o.Total)
            .Take(MaxHotelCandidates)
            .ToList();

        var prices = options.Select(o => o.Room.PricePerNight).Distinct().OrderBy(p => p).ToList();
        foreach (var option in options)
        {
            var rank = prices.Count <= 1 ? 0.5 : prices.IndexOf(option.Room.PricePerNight) / (double)(prices.Count - 1);
            option.Fit = input.AccommodationPreference.ToLowerInvariant() switch
            {
                "luxury" => rank,
                "budget" => 1 - rank,
                _ => 1 - Math.Abs(rank - 0.5) * 2
            };
        }

        return options;
    }

    public static List<PackageOption> BuildPackageOptions(PlanningInput input, IEnumerable<CatalogPackageMatch> packages, ISet<int>? excludedPackages = null) =>
        packages
            .Where(p => p.DurationDays <= input.Days
                        && (p.MaxTravelers <= 0 || p.MaxTravelers >= input.Travelers)
                        && excludedPackages?.Contains(p.Id) != true)
            .Select(p => new PackageOption
            {
                Package = p,
                Total = p.PricePerPerson * input.Travelers,
                InterestMatch = InterestMatch(p, input.Interests)
            })
            .OrderBy(o => o.Total)
            .Take(MaxPackageCandidates)
            .ToList();

    public static List<TransportOption> BuildTransportOptions(PlanningInput input, IEnumerable<CatalogTransportMatch> transport, string destinationName) =>
        transport
            .Where(t => t.PricePerPerson >= 0
                        && !t.FromLocation.Contains(destinationName, StringComparison.OrdinalIgnoreCase)
                        && (t.Capacity <= 0 || t.Capacity >= input.Travelers))
            .Select(t => new TransportOption
            {
                Transport = t,
                Trips = 2,
                Total = t.PricePerPerson * input.Travelers * 2
            })
            .OrderBy(o => o.Total)
            .Take(MaxTransportCandidates)
            .ToList();

    public static BudgetPlanResult Select(
        PlanningInput input,
        IReadOnlyList<HotelOption> hotels,
        IReadOnlyList<PackageOption> packages,
        IReadOnlyList<TransportOption> transport)
    {
        if (input.Nights > 0 && hotels.Count == 0)
        {
            return new BudgetPlanResult
            {
                FailureReason = $"No approved hotel room in the catalog can host {input.Travelers} traveler(s) for this destination."
            };
        }

        IReadOnlyList<HotelOption?> hotelChoices = input.Nights > 0 ? hotels.Cast<HotelOption?>().ToList() : [null];
        var packageChoices = packages.Cast<PackageOption?>().Append(null).ToList();
        var transportChoices = transport.Cast<TransportOption?>().Append(null).ToList();

        (HotelOption? H, PackageOption? P, TransportOption? T, double Score, decimal Total)? best = null;
        decimal? cheapest = null;
        string? cheapestDescription = null;

        foreach (var hotel in hotelChoices)
        foreach (var package in packageChoices)
        foreach (var option in transportChoices)
        {
            var total = (hotel?.Total ?? 0) + (package?.Total ?? 0) + (option?.Total ?? 0);
            if (total <= 0)
                continue;

            if (package is null && option is null && (cheapest is null || total < cheapest))
            {
                cheapest = total;
                cheapestDescription = hotel is null
                    ? null
                    : $"{hotel.Room.HotelName} ({hotel.Room.RoomName}) for {input.Nights} night(s)" + (hotel.Rooms > 1 ? $" × {hotel.Rooms} rooms" : "");
            }

            if (total > input.Budget)
                continue;

            var score = Score(input, hotel, package, option, total);
            if (best is null || score > best.Value.Score + 1e-9 || (Math.Abs(score - best.Value.Score) < 1e-9 && total < best.Value.Total))
                best = (hotel, package, option, score, total);
        }

        if (best is null)
        {
            return new BudgetPlanResult
            {
                CheapestTotal = cheapest,
                CheapestDescription = cheapestDescription,
                FailureReason = cheapest is null
                    ? "Nothing in the catalog matches this trip."
                    : $"The cheapest option costs Rs. {cheapest:N0}, which is more than the Rs. {input.Budget:N0} budget."
            };
        }

        var (h, p, t, s, chosenTotal) = best.Value;
        var selection = new PlanSelection { Hotel = h, Package = p, Transport = t, Score = s };

        if (h is not null)
        {
            selection.AlternativeHotels.AddRange(hotels
                .Where(o => o != h && o.Room.HotelId != h.Room.HotelId && chosenTotal - h.Total + o.Total <= input.Budget)
                .OrderByDescending(o => o.Fit)
                .ThenBy(o => o.Total)
                .DistinctBy(o => o.Room.HotelId)
                .Take(2));
            if (selection.AlternativeHotels.Count < 2)
            {
                selection.AlternativeHotels.AddRange(hotels
                    .Where(o => o != h && !selection.AlternativeHotels.Contains(o) && chosenTotal - h.Total + o.Total <= input.Budget)
                    .OrderByDescending(o => o.Fit)
                    .Take(2 - selection.AlternativeHotels.Count));
            }
        }

        selection.AlternativePackages.AddRange(packages
            .Where(o => o != p && chosenTotal - (p?.Total ?? 0) + o.Total <= input.Budget)
            .OrderByDescending(o => o.InterestMatch)
            .ThenBy(o => o.Total)
            .Take(2));

        selection.AlternativeTransport.AddRange(transport
            .Where(o => o != t && chosenTotal - (t?.Total ?? 0) + o.Total <= input.Budget)
            .OrderBy(o => o.Total)
            .Take(2));

        return new BudgetPlanResult { Selection = selection };
    }

    public static double InterestMatch(CatalogPackageMatch package, IReadOnlyList<string> interests)
    {
        if (package.Activities.Count == 0 || interests.Count == 0)
            return 0;

        var matching = package.Activities.Count(a => interests.Any(i =>
            (a.Category is not null && a.Category.Equals(i, StringComparison.OrdinalIgnoreCase)) ||
            a.Title.Contains(i, StringComparison.OrdinalIgnoreCase) ||
            (a.Description?.Contains(i, StringComparison.OrdinalIgnoreCase) ?? false)));
        return matching / (double)package.Activities.Count;
    }

    private static double Score(PlanningInput input, HotelOption? hotel, PackageOption? package, TransportOption? transport, decimal total)
    {
        var score = 0.0;

        if (hotel is not null)
        {
            score += 2 * hotel.Fit;
            score += 0.5 * (hotel.Room.AverageRating ?? 3.5) / 5;
            if (hotel.Rooms > 1)
                score -= 1;
        }

        if (package is not null)
            score += 1.5 + 2 * package.InterestMatch + 0.25 * (package.Package.AverageRating ?? 3.5) / 5;

        if (transport is not null)
        {
            score += 0.75;
            if (!string.IsNullOrWhiteSpace(input.TransportPreference))
            {
                score += transport.Transport.Mode.ToString().Equals(input.TransportPreference, StringComparison.OrdinalIgnoreCase)
                    ? 1
                    : -0.5;
            }
        }

        var utilisation = input.Budget > 0 ? (double)(total / input.Budget) : 0;
        score += input.AccommodationPreference.Equals("budget", StringComparison.OrdinalIgnoreCase)
            ? -0.5 * utilisation
            : 0.5 * utilisation;

        return score;
    }
}
