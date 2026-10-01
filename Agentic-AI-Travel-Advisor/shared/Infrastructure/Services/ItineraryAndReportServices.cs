using TravelAdvisor.Core.Common;
using TravelAdvisor.Core.DTOs.Itineraries;
using TravelAdvisor.Core.DTOs.Reports;
using TravelAdvisor.Core.Entities;
using TravelAdvisor.Core.Enums;
using TravelAdvisor.Core.Interfaces.Repositories;
using TravelAdvisor.Core.Interfaces.Services;

namespace TravelAdvisor.Infrastructure.Services;

public sealed class ItineraryService(
    IItineraryRepository itineraries,
    IDestinationRepository destinations,
    IUnitOfWork unitOfWork) : IItineraryService
{
    public async Task<ItineraryDetailDto> CreateAsync(UserContext caller, CreateItineraryRequest request, CancellationToken cancellationToken = default)
    {
        if (request.DestinationId.HasValue && !await destinations.ExistsAsync(request.DestinationId.Value, cancellationToken))
            throw new BusinessRuleException("Destination not found.");

        var itinerary = new Itinerary
        {
            UserId = caller.UserId,
            Title = request.Title.Trim(),
            StartDate = DateTime.SpecifyKind(request.StartDate, DateTimeKind.Utc),
            EndDate = DateTime.SpecifyKind(request.EndDate, DateTimeKind.Utc),
            Status = ItineraryStatus.Draft,
            EstimatedCost = request.EstimatedCost,
            DestinationId = request.DestinationId,
            Summary = Ownership.Clean(request.Summary),
            Items = request.Items.Select((item, index) => new ItineraryItem
            {
                DayNumber = item.DayNumber,
                Title = item.Title.Trim(),
                Description = Ownership.Clean(item.Description),
                StartTime = item.StartTime,
                SortOrder = item.SortOrder != 0 ? item.SortOrder : index
            }).ToList()
        };

        itineraries.Add(itinerary);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return (await itineraries.GetForUserAsync(itinerary.Id, caller.UserId, cancellationToken))!.ToDetailDto();
    }

    public async Task<List<ItineraryDto>> ListAsync(UserContext caller, CancellationToken cancellationToken = default) =>
        (await itineraries.ListByUserAsync(caller.UserId, cancellationToken)).Select(i => i.ToDto()).ToList();

    public async Task<ItineraryDetailDto> GetAsync(UserContext caller, int id, CancellationToken cancellationToken = default)
    {
        var itinerary = await itineraries.GetForUserAsync(id, caller.UserId, cancellationToken)
                        ?? throw new NotFoundException("Itinerary not found.");
        return itinerary.ToDetailDto();
    }

    public async Task DeleteAsync(UserContext caller, int id, CancellationToken cancellationToken = default)
    {
        var itinerary = await itineraries.GetForUserAsync(id, caller.UserId, cancellationToken)
                        ?? throw new NotFoundException("Itinerary not found.");
        itineraries.Remove(itinerary);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public sealed class ReportService(IReportRepository reports, TimeProvider clock) : IReportService
{
    public const int MaxRangeMonths = 36;

    public async Task<StatisticsDto> GetStatisticsAsync(UserContext caller, StatisticsQuery query, CancellationToken cancellationToken = default)
    {
        var today = clock.GetUtcNow().UtcDateTime.Date;
        var to = query.To.HasValue ? DateTime.SpecifyKind(query.To.Value.Date, DateTimeKind.Utc) : today.AddDays(1);
        var from = query.From.HasValue
            ? DateTime.SpecifyKind(query.From.Value.Date, DateTimeKind.Utc)
            : new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-11);
        if (to <= from)
            throw new BusinessRuleException("'to' must be after 'from'.");
        if (from < to.AddMonths(-MaxRangeMonths))
            throw new BusinessRuleException($"The range can be at most {MaxRangeMonths} months.");

        var scope = BookingService.FilterFor(caller);
        var facts = await reports.GetBookingFactsAsync(new BookingFilter
        {
            GuestUserId = scope.GuestUserId,
            HotelOwnerId = scope.HotelOwnerId,
            AgentId = scope.AgentId,
            CreatedFrom = from,
            CreatedTo = to
        }, cancellationToken);
        var ratings = await reports.GetReviewFactsAsync(scope.HotelOwnerId, scope.AgentId, cancellationToken);

        static bool Earns(BookingFact f) => f.Status is BookingStatus.Confirmed or BookingStatus.Completed;
        var earning = facts.Where(Earns).ToList();
        var revenue = earning.Sum(f => f.TotalPrice);

        var dto = new StatisticsDto
        {
            From = from,
            To = to,
            TotalBookings = facts.Count,
            BookingsByStatus = Enum.GetValues<BookingStatus>().ToDictionary(s => s, s => facts.Count(f => f.Status == s)),
            Revenue = revenue,
            AverageBookingValue = earning.Count == 0 ? 0 : Math.Round(revenue / earning.Count, 2),
            CancellationRate = facts.Count == 0 ? 0 : Math.Round((double)facts.Count(f => f.Status == BookingStatus.Cancelled) / facts.Count, 4),
            TotalGuests = facts.Where(f => f.Status != BookingStatus.Cancelled).Sum(f => f.Guests),
            AverageRating = ratings.Count == 0 ? null : Math.Round(ratings.Average(r => r.Rating), 1),
            ReviewCount = ratings.Count
        };

        for (var month = new DateTime(from.Year, from.Month, 1, 0, 0, 0, DateTimeKind.Utc); month < to; month = month.AddMonths(1))
        {
            var inMonth = facts.Where(f => f.CreatedAt >= month && f.CreatedAt < month.AddMonths(1)).ToList();
            dto.Monthly.Add(new MonthlyStatDto
            {
                Month = month.ToString("yyyy-MM"),
                Bookings = inMonth.Count,
                Revenue = inMonth.Where(Earns).Sum(f => f.TotalPrice)
            });
        }

        var listings = facts
            .Where(f => f.Status != BookingStatus.Cancelled)
            .GroupBy(f => f.HotelId.HasValue ? ("Hotel", f.HotelId.Value, f.HotelName) : ("Package", f.PackageId ?? 0, f.PackageTitle))
            .Select(g =>
            {
                var (type, id, name) = g.Key;
                var listingRatings = ratings.Where(r => type == "Hotel" ? r.HotelId == id : r.PackageId == id).ToList();
                return new ListingStatDto
                {
                    Id = id,
                    Type = type,
                    Name = name ?? string.Empty,
                    Bookings = g.Count(),
                    Revenue = g.Where(Earns).Sum(f => f.TotalPrice),
                    AverageRating = listingRatings.Count == 0 ? null : Math.Round(listingRatings.Average(r => r.Rating), 1),
                    ReviewCount = listingRatings.Count
                };
            })
            .OrderByDescending(l => l.Revenue).ThenByDescending(l => l.Bookings).ThenBy(l => l.Name)
            .Take(Math.Clamp(query.Top, 1, 20));
        dto.TopListings.AddRange(listings);

        return dto;
    }

    public async Task<ReportSummaryDto> GetSummaryAsync(UserContext caller, CancellationToken cancellationToken = default)
    {
        var totals = await reports.GetBookingTotalsAsync(BookingService.FilterFor(caller), cancellationToken);
        int Count(BookingStatus status) => totals.FirstOrDefault(t => t.Status == status)?.Count ?? 0;

        var dto = new ReportSummaryDto
        {
            PendingBookings = Count(BookingStatus.Pending),
            ConfirmedBookings = Count(BookingStatus.Confirmed),
            CancelledBookings = Count(BookingStatus.Cancelled),
            CompletedBookings = Count(BookingStatus.Completed),
            Revenue = totals
                .Where(t => t.Status is BookingStatus.Confirmed or BookingStatus.Completed)
                .Sum(t => t.Total)
        };

        if (caller.IsAdmin)
        {
            var counts = await reports.GetCatalogCountsAsync(null, null, cancellationToken);
            dto.UserCount = counts.Users;
            dto.HotelOwnerCount = counts.HotelOwners;
            dto.TravelAgentCount = counts.TravelAgents;
            dto.HotelCount = counts.Hotels;
            dto.PackageCount = counts.Packages;
            dto.PendingHotelApprovals = counts.PendingHotels;
            dto.PendingPackageApprovals = counts.PendingPackages;
        }
        else if (caller.IsHotelOwner)
        {
            var counts = await reports.GetCatalogCountsAsync(caller.UserId, null, cancellationToken);
            dto.HotelCount = counts.Hotels;
            dto.PendingHotelApprovals = counts.PendingHotels;
        }
        else if (caller.IsTravelAgent)
        {
            var counts = await reports.GetCatalogCountsAsync(null, caller.UserId, cancellationToken);
            dto.PackageCount = counts.Packages;
            dto.PendingPackageApprovals = counts.PendingPackages;
        }

        return dto;
    }
}
