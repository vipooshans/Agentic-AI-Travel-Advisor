using Microsoft.EntityFrameworkCore;
using TravelAdvisor.Core.Entities;
using TravelAdvisor.Core.Interfaces.Repositories;
using TravelAdvisor.Infrastructure.Data;

namespace TravelAdvisor.Infrastructure.Repositories;

public sealed class ReviewRepository(AppDbContext context) : IReviewRepository
{
    public Task<Review?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        WithIncludes(context.Reviews).FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public Task<bool> ExistsForBookingAsync(int bookingId, CancellationToken cancellationToken = default) =>
        context.Reviews.AnyAsync(r => r.BookingId == bookingId, cancellationToken);

    public Task<List<Review>> ListAsync(ReviewFilter filter, CancellationToken cancellationToken = default)
    {
        var query = WithIncludes(context.Reviews.AsNoTracking());
        if (filter.HotelId.HasValue)
            query = query.Where(r => r.HotelId == filter.HotelId);
        if (filter.TravelPackageId.HasValue)
            query = query.Where(r => r.TravelPackageId == filter.TravelPackageId);
        if (filter.UserId is not null)
            query = query.Where(r => r.UserId == filter.UserId);
        if (filter.HotelOwnerId is not null)
            query = query.Where(r => r.Hotel != null && r.Hotel.OwnerId == filter.HotelOwnerId);
        if (filter.AgentId is not null)
            query = query.Where(r => r.TravelPackage != null && r.TravelPackage.AgentId == filter.AgentId);
        if (filter.Status.HasValue)
            query = query.Where(r => r.Status == filter.Status);
        return query.OrderByDescending(r => r.CreatedAt).ToListAsync(cancellationToken);
    }

    public void Add(Review review) => context.Reviews.Add(review);

    public void Remove(Review review) => context.Reviews.Remove(review);

    private static IQueryable<Review> WithIncludes(IQueryable<Review> query) =>
        query.Include(r => r.User).Include(r => r.Hotel).Include(r => r.TravelPackage);
}

public sealed class TransportationRepository(AppDbContext context) : ITransportationRepository
{
    public Task<List<Transportation>> SearchAsync(TransportationCriteria criteria, CancellationToken cancellationToken = default)
    {
        var query = WithIncludes(context.Transportation.AsNoTracking());
        if (criteria.ActiveOnly)
            query = query.Where(t => t.IsActive);
        if (criteria.ProviderId is not null)
            query = query.Where(t => t.ProviderId == criteria.ProviderId);
        if (!string.IsNullOrWhiteSpace(criteria.From))
            query = query.Where(t => EF.Functions.ILike(t.FromLocation, $"%{HotelRepository.EscapeLike(criteria.From.Trim())}%"));
        if (!string.IsNullOrWhiteSpace(criteria.To))
            query = query.Where(t => EF.Functions.ILike(t.ToLocation, $"%{HotelRepository.EscapeLike(criteria.To.Trim())}%"));
        if (criteria.DestinationId.HasValue)
            query = query.Where(t => t.DestinationId == criteria.DestinationId);
        if (criteria.TravelPackageId.HasValue)
            query = query.Where(t => t.TravelPackageId == criteria.TravelPackageId);
        if (criteria.Mode.HasValue)
            query = query.Where(t => t.Mode == criteria.Mode);
        if (criteria.MaxPrice.HasValue)
            query = query.Where(t => t.PricePerPerson <= criteria.MaxPrice);
        return query.OrderBy(t => t.PricePerPerson).ThenBy(t => t.Id).ToListAsync(cancellationToken);
    }

    public Task<Transportation?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        WithIncludes(context.Transportation).FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public void Add(Transportation transportation) => context.Transportation.Add(transportation);

    public void Remove(Transportation transportation) => context.Transportation.Remove(transportation);

    private static IQueryable<Transportation> WithIncludes(IQueryable<Transportation> query) =>
        query.Include(t => t.Destination).Include(t => t.TravelPackage);
}

public sealed class PaymentRepository(AppDbContext context) : IPaymentRepository
{
    public Task<List<Payment>> ListByBookingAsync(int bookingId, CancellationToken cancellationToken = default) =>
        context.Payments.Where(p => p.BookingId == bookingId).OrderBy(p => p.CreatedAt).ToListAsync(cancellationToken);

    public Task<Payment?> GetAsync(int bookingId, int paymentId, CancellationToken cancellationToken = default) =>
        context.Payments.FirstOrDefaultAsync(p => p.Id == paymentId && p.BookingId == bookingId, cancellationToken);

    public void Add(Payment payment) => context.Payments.Add(payment);
}

public sealed class UserProfileRepository(AppDbContext context) : IUserProfileRepository
{
    public Task<UserProfile?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default) =>
        context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

    public void Add(UserProfile profile) => context.UserProfiles.Add(profile);
}
