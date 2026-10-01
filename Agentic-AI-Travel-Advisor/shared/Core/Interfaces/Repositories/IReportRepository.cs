using TravelAdvisor.Core.Enums;

namespace TravelAdvisor.Core.Interfaces.Repositories;

public sealed record BookingStatusTotals(BookingStatus Status, int Count, decimal Total);

public sealed record CatalogCounts(
    int Users,
    int HotelOwners,
    int TravelAgents,
    int Hotels,
    int Packages,
    int PendingHotels,
    int PendingPackages);

public interface IReportRepository
{
    Task<List<BookingStatusTotals>> GetBookingTotalsAsync(BookingFilter filter, CancellationToken cancellationToken = default);
    Task<CatalogCounts> GetCatalogCountsAsync(string? hotelOwnerId, string? agentId, CancellationToken cancellationToken = default);
}
