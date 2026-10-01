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

public sealed class ReportService(IReportRepository reports) : IReportService
{
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
