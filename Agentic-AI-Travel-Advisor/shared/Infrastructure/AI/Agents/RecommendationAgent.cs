using TravelAdvisor.Core.DTOs.AI;
using TravelAdvisor.Core.Interfaces;

namespace TravelAdvisor.Infrastructure.AI.Agents;

public class RecommendationAgent
{
    private readonly ICatalogTools _catalog;

    public RecommendationAgent(ICatalogTools catalog)
    {
        _catalog = catalog;
    }

    public async Task<RecommendationResult> RecommendAsync(
        TripRequirements requirements,
        CancellationToken cancellationToken = default)
    {
        var destinations = await _catalog.SearchDestinationsAsync(requirements.Destination, cancellationToken);
        var destination = destinations.FirstOrDefault()
            ?? (await _catalog.SearchDestinationsAsync(null, cancellationToken))
                .FirstOrDefault(d => d.Name.Contains(requirements.Destination!, StringComparison.OrdinalIgnoreCase));

        if (destination is null)
        {
            return new RecommendationResult
            {
                Error = $"I could not find '{requirements.Destination}' in our catalog. Try Ella, Kandy, or Galle."
            };
        }

        var durationDays = requirements.DurationDays ?? 3;
        var nights = Math.Max(1, durationDays - 1);
        var travelers = Math.Max(1, requirements.Travelers);
        var budget = requirements.Budget ?? 0;

        var packages = await _catalog.SearchPackagesAsync(
            destination.Id,
            destination.Name,
            budget,
            durationDays,
            cancellationToken);

        if (packages.Count == 0)
        {
            packages = await _catalog.SearchPackagesAsync(
                destination.Id,
                destination.Name,
                null,
                durationDays,
                cancellationToken);
        }

        var package = packages
            .OrderBy(p => Math.Abs(p.DurationDays - durationDays))
            .ThenBy(p => p.Price)
            .FirstOrDefault(p => p.Price <= budget)
            ?? packages.OrderBy(p => p.Price).FirstOrDefault();

        var remainingForHotel = package is not null ? budget - package.Price : budget;
        var maxPerNight = remainingForHotel > 0 ? remainingForHotel / nights : (decimal?)null;

        var hotels = await _catalog.SearchHotelsAsync(destination.Name, travelers, maxPerNight, cancellationToken);
        if (hotels.Count == 0)
            hotels = await _catalog.SearchHotelsAsync(destination.Name, travelers, null, cancellationToken);

        var hotel = PickHotel(hotels, requirements.AccommodationPreference, nights, remainingForHotel);

        return new RecommendationResult
        {
            Destination = destination,
            Hotel = hotel,
            Package = package,
            Nights = nights
        };
    }

    private static CatalogHotelMatch? PickHotel(
        List<CatalogHotelMatch> hotels,
        string? preference,
        int nights,
        decimal remainingForHotel)
    {
        if (hotels.Count == 0)
            return null;

        IEnumerable<CatalogHotelMatch> inBudget = hotels.Where(h => h.PricePerNight * nights <= remainingForHotel);
        if (!inBudget.Any())
            inBudget = hotels;

        var ranked = preference?.ToLowerInvariant() switch
        {
            "luxury" => inBudget.OrderByDescending(h => h.PricePerNight),
            "budget" => inBudget.OrderBy(h => h.PricePerNight),
            _ => inBudget.OrderBy(h => Math.Abs(h.PricePerNight - MidPrice(hotels)))
        };

        return ranked.First();
    }

    private static decimal MidPrice(List<CatalogHotelMatch> hotels)
    {
        var ordered = hotels.Select(h => h.PricePerNight).OrderBy(p => p).ToList();
        return ordered[ordered.Count / 2];
    }
}

public class RecommendationResult
{
    public string? Error { get; set; }
    public CatalogDestinationMatch? Destination { get; set; }
    public CatalogHotelMatch? Hotel { get; set; }
    public CatalogPackageMatch? Package { get; set; }
    public int Nights { get; set; }
}
