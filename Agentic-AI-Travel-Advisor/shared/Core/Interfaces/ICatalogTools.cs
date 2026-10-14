using TravelAdvisor.Core.DTOs.AI;
using TravelAdvisor.Core.Enums;

namespace TravelAdvisor.Core.Interfaces;

/// <summary>Read-only catalog queries used by AI tools. Only approved, active listings are returned.</summary>
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

    Task<List<CatalogActivityMatch>> SearchActivitiesAsync(
        int? destinationId,
        string? destinationName,
        string? category,
        decimal? maxPrice,
        CancellationToken cancellationToken = default);

    Task<CatalogHotelMatch?> GetRoomAsync(int roomId, CancellationToken cancellationToken = default);

    Task<CatalogPackageMatch?> GetPackageAsync(int packageId, CancellationToken cancellationToken = default);

    Task<List<CatalogTransportMatch>> SearchTransportationAsync(
        int? destinationId,
        string? destinationName,
        string? from,
        TransportMode? mode,
        decimal? maxPricePerPerson,
        CancellationToken cancellationToken = default);
}
