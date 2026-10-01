using TravelAdvisor.Core.Common;
using TravelAdvisor.Core.DTOs.Bookings;
using TravelAdvisor.Core.Entities;
using TravelAdvisor.Core.Enums;
using TravelAdvisor.Core.Interfaces.Repositories;
using TravelAdvisor.Core.Interfaces.Services;
using TravelAdvisor.Infrastructure.Helpers;
using TravelAdvisor.Infrastructure.Repositories;

namespace TravelAdvisor.Infrastructure.Services;

public sealed class BookingService(
    IBookingRepository bookings,
    IRoomRepository rooms,
    IPackageRepository packages,
    IRoomAvailabilityRepository availability,
    ISystemSettingRepository settings,
    IPaymentRepository payments,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : IBookingService
{
    public const int MaxNights = 30;
    public const int DefaultMaxAdvanceDays = 365;
    public const int DefaultCancellationCutoffHours = 24;

    private sealed record Draft(Booking Booking, int Nights, int? RemainingPlaces);

    public async Task<BookingDto> CreateAsync(UserContext caller, CreateBookingRequest request, CancellationToken cancellationToken = default)
    {
        if (request.RoomId.HasValue == request.TravelPackageId.HasValue)
            throw new BusinessRuleException("Specify either a room or a travel package, not both.");

        var bookingId = await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            // The row lock makes concurrent requests for the same room/package queue up, so the
            // availability checks below see every booking committed before them. The exclusion
            // constraint on Bookings is still the final guard for rooms.
            Draft draft;
            if (request.RoomId.HasValue)
            {
                await bookings.LockRoomAsync(request.RoomId.Value, ct);
                draft = await BuildRoomDraftAsync(caller.UserId, request.RoomId.Value, request.CheckIn, request.CheckOut, request.Guests, ct);
            }
            else
            {
                await bookings.LockPackageAsync(request.TravelPackageId!.Value, ct);
                draft = await BuildPackageDraftAsync(caller.UserId, request.TravelPackageId.Value, request.CheckIn, request.Guests, ct);
            }

            draft.Booking.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
            bookings.Add(draft.Booking);
            await unitOfWork.SaveChangesAsync(ct);
            return draft.Booking.Id;
        }, cancellationToken: cancellationToken);

        return (await bookings.GetByIdAsync(bookingId, cancellationToken))!.ToDto();
    }

    public async Task<AvailabilityQuoteDto> CheckAvailabilityAsync(UserContext? caller, AvailabilityQuery query, CancellationToken cancellationToken = default)
    {
        if (query.RoomId.HasValue == query.TravelPackageId.HasValue)
            throw new BusinessRuleException("Specify either a room or a travel package, not both.");

        var quote = new AvailabilityQuoteDto
        {
            RoomId = query.RoomId,
            TravelPackageId = query.TravelPackageId,
            CheckIn = ToUtcDate(query.CheckIn),
            CheckOut = query.CheckOut.HasValue ? ToUtcDate(query.CheckOut.Value) : ToUtcDate(query.CheckIn),
            Guests = query.Guests
        };

        try
        {
            var draft = query.RoomId.HasValue
                ? await BuildRoomDraftAsync(caller?.UserId, query.RoomId.Value, query.CheckIn, query.CheckOut, query.Guests, cancellationToken)
                : await BuildPackageDraftAsync(caller?.UserId, query.TravelPackageId!.Value, query.CheckIn, query.Guests, cancellationToken);

            quote.Available = true;
            quote.CheckIn = draft.Booking.CheckIn;
            quote.CheckOut = draft.Booking.CheckOut;
            quote.Nights = draft.Nights;
            quote.TotalPrice = draft.Booking.TotalPrice;
            quote.RemainingPlaces = draft.RemainingPlaces;
        }
        catch (Exception ex) when (ex is BusinessRuleException or ConflictException)
        {
            quote.Available = false;
            quote.Reason = ex.Message;
        }

        return quote;
    }

    public async Task<List<BookingDto>> ListAsync(UserContext caller, CancellationToken cancellationToken = default) =>
        (await bookings.ListAsync(FilterFor(caller), cancellationToken)).Select(b => b.ToDto()).ToList();

    public async Task<BookingDto> GetAsync(UserContext caller, int id, CancellationToken cancellationToken = default)
    {
        var booking = await bookings.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Booking not found.");
        if (!CanAccess(caller, booking))
            throw new ForbiddenException();
        return booking.ToDto();
    }

    public async Task<BookingDto> UpdateStatusAsync(UserContext caller, int id, UpdateBookingStatusRequest request, CancellationToken cancellationToken = default)
    {
        var booking = await bookings.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Booking not found.");
        if (!CanAccess(caller, booking))
            throw new ForbiddenException();
        if (booking.Status == request.Status)
            throw new BusinessRuleException($"Booking is already {booking.Status}.");

        var now = clock.GetUtcNow().UtcDateTime;
        var isGuest = booking.UserId == caller.UserId && caller.IsUser;

        if (isGuest && booking.Status == BookingStatus.Confirmed && request.Status == BookingStatus.Cancelled)
        {
            var cutoffHours = await settings.GetIntAsync(SystemSettingKeys.GuestCancellationCutoffHours, DefaultCancellationCutoffHours, cancellationToken);
            if (!BookingStatusHelper.IsBeforeCancellationCutoff(booking.CheckIn, now, cutoffHours))
                throw new BusinessRuleException(
                    $"Confirmed bookings can only be cancelled more than {cutoffHours} hours before check-in. Please contact the provider.");
        }
        else if (!BookingStatusHelper.CanTransition(booking.Status, request.Status, caller.Role, isGuest))
        {
            throw new BusinessRuleException($"Cannot change status from {booking.Status} to {request.Status}.");
        }

        if (request.Status == BookingStatus.Completed && !BookingStatusHelper.CanComplete(booking.CheckIn, now))
            throw new BusinessRuleException("A booking can only be marked completed on or after its check-in date.");

        booking.Status = request.Status;
        if (request.Status == BookingStatus.Cancelled)
        {
            booking.CancelledAt = now;
            // Simulated payments: money taken is refunded and anything still awaiting confirmation is voided.
            foreach (var payment in await payments.ListByBookingAsync(booking.Id, cancellationToken))
            {
                if (payment.Status == PaymentStatus.Completed)
                    payment.Status = PaymentStatus.Refunded;
                else if (payment.Status == PaymentStatus.Pending)
                    payment.Status = PaymentStatus.Failed;
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return booking.ToDto();
    }

    internal static BookingFilter FilterFor(UserContext caller) => caller.Role switch
    {
        RoleNames.Admin => new BookingFilter(),
        RoleNames.HotelOwner => new BookingFilter { HotelOwnerId = caller.UserId },
        RoleNames.TravelAgent => new BookingFilter { AgentId = caller.UserId },
        _ => new BookingFilter { GuestUserId = caller.UserId }
    };

    internal static DateTime ToUtcDate(DateTime value) => DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);

    internal static bool CanAccess(UserContext caller, Booking booking) => caller.Role switch
    {
        RoleNames.Admin => true,
        RoleNames.HotelOwner => booking.Room?.Hotel.OwnerId == caller.UserId,
        RoleNames.TravelAgent => booking.TravelPackage?.AgentId == caller.UserId,
        _ => booking.UserId == caller.UserId
    };

    private async Task ValidateStartDateAsync(DateTime checkIn, CancellationToken cancellationToken)
    {
        var today = clock.GetUtcNow().UtcDateTime.Date;
        if (checkIn < today)
            throw new BusinessRuleException("Check-in date cannot be in the past.",
                new Dictionary<string, string[]> { ["checkIn"] = ["Check-in date cannot be in the past."] });

        var maxAdvanceDays = await settings.GetIntAsync(SystemSettingKeys.MaxAdvanceBookingDays, DefaultMaxAdvanceDays, cancellationToken);
        if (checkIn > today.AddDays(maxAdvanceDays))
            throw new BusinessRuleException($"Bookings can be made at most {maxAdvanceDays} days in advance.",
                new Dictionary<string, string[]> { ["checkIn"] = [$"Bookings can be made at most {maxAdvanceDays} days in advance."] });
    }

    private static void ValidateGuests(int guests, int capacity, string unit)
    {
        if (guests < 1)
            throw new BusinessRuleException("At least one guest is required.",
                new Dictionary<string, string[]> { ["guests"] = ["At least one guest is required."] });
        if (guests > capacity)
            throw new BusinessRuleException($"This {unit} allows at most {capacity} guests.",
                new Dictionary<string, string[]> { ["guests"] = [$"This {unit} allows at most {capacity} guests."] });
    }

    private async Task<Draft> BuildRoomDraftAsync(string? userId, int roomId, DateTime checkInRaw, DateTime? checkOutRaw, int guests, CancellationToken cancellationToken)
    {
        if (!checkOutRaw.HasValue)
            throw new BusinessRuleException("CheckOut is required for room bookings.",
                new Dictionary<string, string[]> { ["checkOut"] = ["CheckOut is required for room bookings."] });

        var checkIn = ToUtcDate(checkInRaw);
        var checkOut = ToUtcDate(checkOutRaw.Value);
        if (checkOut <= checkIn)
            throw new BusinessRuleException("CheckOut must be after CheckIn.",
                new Dictionary<string, string[]> { ["checkOut"] = ["CheckOut must be after CheckIn."] });

        var nights = (checkOut - checkIn).Days;
        if (nights > MaxNights)
            throw new BusinessRuleException($"A stay can be at most {MaxNights} nights.",
                new Dictionary<string, string[]> { ["checkOut"] = [$"A stay can be at most {MaxNights} nights."] });

        await ValidateStartDateAsync(checkIn, cancellationToken);

        var room = await rooms.GetByIdAsync(roomId, cancellationToken) ?? throw new NotFoundException("Room not found.");
        if (room.Hotel.ApprovalStatus != ApprovalStatus.Approved)
            throw new BusinessRuleException("Hotel is not approved for booking.");
        if (!room.IsAvailable)
            throw new ConflictException("Room is not available for booking.");
        ValidateGuests(guests, room.Capacity, "room");

        var from = DateOnly.FromDateTime(checkIn);
        var to = DateOnly.FromDateTime(checkOut);
        var overrides = await availability.ListAsync(room.Id, from, to, cancellationToken);
        var blocked = overrides.Where(o => o.IsBlocked).Select(o => o.Date.ToString("yyyy-MM-dd")).ToList();
        if (blocked.Count > 0)
            throw new ConflictException($"Room is not available on {string.Join(", ", blocked)}.");

        if (await bookings.HasRoomOverlapAsync(room.Id, checkIn, checkOut, cancellationToken: cancellationToken))
            throw new ConflictException("Room is already booked for the selected dates.");

        var priceByDate = overrides.Where(o => o.PriceOverride.HasValue).ToDictionary(o => o.Date, o => o.PriceOverride!.Value);
        decimal total = 0;
        for (var night = from; night < to; night = night.AddDays(1))
            total += priceByDate.TryGetValue(night, out var price) ? price : room.PricePerNight;

        return new Draft(new Booking
        {
            UserId = userId ?? string.Empty,
            RoomId = room.Id,
            CheckIn = checkIn,
            CheckOut = checkOut,
            Guests = guests,
            Status = BookingStatus.Pending,
            TotalPrice = total
        }, nights, null);
    }

    private async Task<Draft> BuildPackageDraftAsync(string? userId, int packageId, DateTime checkInRaw, int guests, CancellationToken cancellationToken)
    {
        var checkIn = ToUtcDate(checkInRaw);
        await ValidateStartDateAsync(checkIn, cancellationToken);

        var package = await packages.GetByIdAsync(packageId, cancellationToken: cancellationToken)
                      ?? throw new NotFoundException("Travel package not found.");
        if (package.ApprovalStatus != ApprovalStatus.Approved)
            throw new BusinessRuleException("Package is not approved for booking.");
        ValidateGuests(guests, package.MaxTravelers, "package");

        var remaining = package.MaxTravelers - await bookings.CountPackageGuestsAsync(package.Id, checkIn, cancellationToken);
        if (guests > remaining)
            throw new ConflictException(remaining <= 0
                ? "This package is fully booked for the selected date."
                : $"Only {remaining} place(s) left for this package on the selected date.");

        if (userId is not null && await bookings.HasActivePackageBookingAsync(userId, package.Id, checkIn, cancellationToken))
            throw new ConflictException("You already have a booking for this package on the selected date.");

        return new Draft(new Booking
        {
            UserId = userId ?? string.Empty,
            TravelPackageId = package.Id,
            CheckIn = checkIn,
            CheckOut = checkIn.AddDays(package.DurationDays),
            Guests = guests,
            Status = BookingStatus.Pending,
            TotalPrice = (package.Price + package.Activities.Sum(a => a.Price)) * guests
        }, package.DurationDays, remaining - guests);
    }
}
