using Microsoft.EntityFrameworkCore;
using TravelAdvisor.Core.Entities;
using TravelAdvisor.Core.Enums;
using TravelAdvisor.Infrastructure.Data;

namespace TravelAdvisor.Infrastructure.Helpers;

public static class BookingHelper
{
    public static async Task<(bool Valid, string? Error, decimal TotalPrice, DateTime CheckOut)> ValidateAndPriceAsync(
        AppDbContext context,
        int? roomId,
        int? travelPackageId,
        DateTime checkIn,
        DateTime? checkOut)
    {
        if (roomId.HasValue == travelPackageId.HasValue)
        {
            return (false, "Specify either a room or a travel package, not both.", 0, default);
        }

        if (roomId.HasValue)
        {
            if (!checkOut.HasValue)
                return (false, "CheckOut is required for room bookings.", 0, default);

            if (checkOut.Value <= checkIn)
                return (false, "CheckOut must be after CheckIn.", 0, default);

            var room = await context.Rooms
                .Include(r => r.Hotel)
                .FirstOrDefaultAsync(r => r.Id == roomId.Value);

            if (room is null)
                return (false, "Room not found.", 0, default);

            if (!room.IsAvailable)
                return (false, "Room is not available.", 0, default);

            var hasOverlap = await context.Bookings.AnyAsync(b =>
                b.RoomId == roomId.Value &&
                b.Status != BookingStatus.Cancelled &&
                b.CheckIn < checkOut.Value &&
                b.CheckOut > checkIn);

            if (hasOverlap)
                return (false, "Room is already booked for the selected dates.", 0, default);

            var nights = (checkOut.Value.Date - checkIn.Date).Days;
            if (nights < 1) nights = 1;
            var total = room.PricePerNight * nights;
            return (true, null, total, checkOut.Value);
        }

        var package = await context.TravelPackages
            .Include(p => p.Activities)
            .FirstOrDefaultAsync(p => p.Id == travelPackageId!.Value);

        if (package is null)
            return (false, "Travel package not found.", 0, default);

        var packageCheckOut = checkIn.Date.AddDays(package.DurationDays);
        var activityTotal = package.Activities.Sum(a => a.Price);
        var packageTotal = package.Price + activityTotal;

        return (true, null, packageTotal, packageCheckOut);
    }
}
