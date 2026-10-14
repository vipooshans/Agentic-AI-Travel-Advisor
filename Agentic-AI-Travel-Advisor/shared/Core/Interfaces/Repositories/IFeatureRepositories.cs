using TravelAdvisor.Core.Entities;
using TravelAdvisor.Core.Enums;

namespace TravelAdvisor.Core.Interfaces.Repositories;

/// <summary>Null fields are not filtered.</summary>
public sealed class ReviewFilter
{
    public int? HotelId { get; init; }
    public int? TravelPackageId { get; init; }
    public string? UserId { get; init; }
    public string? HotelOwnerId { get; init; }
    public string? AgentId { get; init; }
    public ReviewStatus? Status { get; init; }
}

public interface IReviewRepository
{
    Task<Review?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsForBookingAsync(int bookingId, CancellationToken cancellationToken = default);
    Task<List<Review>> ListAsync(ReviewFilter filter, CancellationToken cancellationToken = default);
    void Add(Review review);
    void Remove(Review review);
}

public sealed class TransportationCriteria
{
    public string? From { get; init; }
    public string? To { get; init; }
    public int? DestinationId { get; init; }
    public int? TravelPackageId { get; init; }
    public TransportMode? Mode { get; init; }
    public decimal? MaxPrice { get; init; }
    public string? ProviderId { get; init; }
    public bool ActiveOnly { get; init; } = true;
}

public interface ITransportationRepository
{
    Task<List<Transportation>> SearchAsync(TransportationCriteria criteria, CancellationToken cancellationToken = default);
    Task<Transportation?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    void Add(Transportation transportation);
    void Remove(Transportation transportation);
}

public interface IPaymentRepository
{
    Task<List<Payment>> ListByBookingAsync(int bookingId, CancellationToken cancellationToken = default);
    Task<Payment?> GetAsync(int bookingId, int paymentId, CancellationToken cancellationToken = default);
    void Add(Payment payment);
}

public interface IUserProfileRepository
{
    Task<UserProfile?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    void Add(UserProfile profile);
}
