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

public sealed record BookingFact(
    BookingStatus Status,
    decimal TotalPrice,
    int Guests,
    DateTime CreatedAt,
    int? HotelId,
    string? HotelName,
    int? PackageId,
    string? PackageTitle);

public sealed record ReviewFact(int? HotelId, int? PackageId, int Rating);

public interface IReportRepository
{
    Task<List<BookingStatusTotals>> GetBookingTotalsAsync(BookingFilter filter, CancellationToken cancellationToken = default);
    Task<CatalogCounts> GetCatalogCountsAsync(string? hotelOwnerId, string? agentId, CancellationToken cancellationToken = default);

    /// <summary>One lightweight row per booking matching the filter (including its CreatedFrom/CreatedTo range).</summary>
    Task<List<BookingFact>> GetBookingFactsAsync(BookingFilter filter, CancellationToken cancellationToken = default);

    /// <summary>Visible reviews, optionally limited to one owner's hotels or one agent's packages.</summary>
    Task<List<ReviewFact>> GetReviewFactsAsync(string? hotelOwnerId, string? agentId, CancellationToken cancellationToken = default);
}
