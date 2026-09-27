using TravelAdvisor.Core.DTOs.AI;

namespace TravelAdvisor.Core.Interfaces;

public interface ICatalogTools
{
    Task<List<CatalogDestinationMatch>> SearchDestinationsAsync(string? query, CancellationToken cancellationToken = default);

    Task<List<string>> GetDestinationNamesAsync(CancellationToken cancellationToken = default);

    Task<List<CatalogHotelMatch>> SearchHotelsAsync(
        string city,
        int minCapacity,
        decimal? maxPricePerNight,
        CancellationToken cancellationToken = default);

    Task<List<CatalogPackageMatch>> SearchPackagesAsync(
        int? destinationId,
        string? destinationName,
        decimal? maxPrice,
        int? maxDurationDays,
        CancellationToken cancellationToken = default);
}
