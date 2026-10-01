using TravelAdvisor.Core.Entities;
using TravelAdvisor.Core.Enums;

namespace TravelAdvisor.Infrastructure.Helpers;

public static class BookingStatusHelper
{
    public static bool CanTransition(BookingStatus from, BookingStatus to, string role, bool isGuest)
    {
        return (from, to) switch
        {
            (BookingStatus.Pending, BookingStatus.Confirmed) => role is RoleNames.Admin or RoleNames.HotelOwner or RoleNames.TravelAgent,
            (BookingStatus.Pending, BookingStatus.Cancelled) => isGuest || role is RoleNames.Admin or RoleNames.HotelOwner or RoleNames.TravelAgent,
            (BookingStatus.Confirmed, BookingStatus.Completed) => role is RoleNames.Admin or RoleNames.HotelOwner or RoleNames.TravelAgent,
            (BookingStatus.Confirmed, BookingStatus.Cancelled) => role is RoleNames.Admin or RoleNames.HotelOwner or RoleNames.TravelAgent,
            _ => false
        };
    }

    /// <summary>
    /// A guest may cancel a confirmed booking only while check-in is more than <paramref name="cutoffHours"/> away.
    /// This sits outside <see cref="CanTransition"/> because it depends on time, not just on role and status.
    /// </summary>
    public static bool IsBeforeCancellationCutoff(DateTime checkIn, DateTime utcNow, int cutoffHours) =>
        checkIn - utcNow > TimeSpan.FromHours(Math.Max(0, cutoffHours));

    /// <summary>A stay cannot be marked completed before it has started.</summary>
    public static bool CanComplete(DateTime checkIn, DateTime utcNow) => utcNow.Date >= checkIn.Date;

    public static bool CanManageListing(Booking booking, string userId, string? role)
    {
        return role switch
        {
            RoleNames.Admin => true,
            RoleNames.HotelOwner => booking.Room?.Hotel.OwnerId == userId,
            RoleNames.TravelAgent => booking.TravelPackage?.AgentId == userId,
            _ => false
        };
    }
}
