using TravelAdvisor.Core.Common;
using TravelAdvisor.Core.DTOs.Bookings;
using TravelAdvisor.Core.Entities;
using TravelAdvisor.Core.Enums;
using TravelAdvisor.Core.Interfaces.Repositories;
using TravelAdvisor.Core.Interfaces.Services;
using TravelAdvisor.Infrastructure.Helpers;

namespace TravelAdvisor.Infrastructure.Services;

public sealed class BookingService(
    IBookingRepository bookings,
    IRoomRepository rooms,
    IPackageRepository packages,
    IUnitOfWork unitOfWork) : IBookingService
{
    public async Task<BookingDto> CreateAsync(UserContext caller, CreateBookingRequest request, CancellationToken cancellationToken = default)
    {
        if (request.RoomId.HasValue == request.TravelPackageId.HasValue)
            throw new BusinessRuleException("Specify either a room or a travel package, not both.");

        var booking = request.RoomId.HasValue
            ? await PriceRoomBookingAsync(caller, request, cancellationToken)
            : await PricePackageBookingAsync(caller, request, cancellationToken);

        bookings.Add(booking);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return (await bookings.GetByIdAsync(booking.Id, cancellationToken))!.ToDto();
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

        var isGuest = booking.UserId == caller.UserId && caller.IsUser;
        if (!BookingStatusHelper.CanTransition(booking.Status, request.Status, caller.Role, isGuest))
            throw new BusinessRuleException($"Cannot change status from {booking.Status} to {request.Status}.");

        booking.Status = request.Status;
        if (request.Status == BookingStatus.Cancelled)
            booking.CancelledAt = DateTime.UtcNow;

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

    private static bool CanAccess(UserContext caller, Booking booking) => caller.Role switch
    {
        RoleNames.Admin => true,
        RoleNames.HotelOwner => booking.Room?.Hotel.OwnerId == caller.UserId,
        RoleNames.TravelAgent => booking.TravelPackage?.AgentId == caller.UserId,
        _ => booking.UserId == caller.UserId
    };

    private async Task<Booking> PriceRoomBookingAsync(UserContext caller, CreateBookingRequest request, CancellationToken cancellationToken)
    {
        if (!request.CheckOut.HasValue)
            throw new BusinessRuleException("CheckOut is required for room bookings.");
        if (request.CheckOut.Value <= request.CheckIn)
            throw new BusinessRuleException("CheckOut must be after CheckIn.");

        var room = await rooms.GetByIdAsync(request.RoomId!.Value, cancellationToken)
                   ?? throw new BusinessRuleException("Room not found.");
        if (room.Hotel.ApprovalStatus != ApprovalStatus.Approved)
            throw new BusinessRuleException("Hotel is not approved for booking.");
        if (!room.IsAvailable)
            throw new BusinessRuleException("Room is not available.");
        if (await bookings.HasRoomOverlapAsync(room.Id, request.CheckIn, request.CheckOut.Value, cancellationToken: cancellationToken))
            throw new ConflictException("Room is already booked for the selected dates.");

        var nights = Math.Max(1, (request.CheckOut.Value.Date - request.CheckIn.Date).Days);
        return new Booking
        {
            UserId = caller.UserId,
            RoomId = room.Id,
            CheckIn = request.CheckIn,
            CheckOut = request.CheckOut.Value,
            Status = BookingStatus.Pending,
            TotalPrice = room.PricePerNight * nights
        };
    }

    private async Task<Booking> PricePackageBookingAsync(UserContext caller, CreateBookingRequest request, CancellationToken cancellationToken)
    {
        var package = await packages.GetByIdAsync(request.TravelPackageId!.Value, cancellationToken: cancellationToken)
                      ?? throw new BusinessRuleException("Travel package not found.");
        if (package.ApprovalStatus != ApprovalStatus.Approved)
            throw new BusinessRuleException("Package is not approved for booking.");

        return new Booking
        {
            UserId = caller.UserId,
            TravelPackageId = package.Id,
            CheckIn = request.CheckIn,
            CheckOut = request.CheckIn.Date.AddDays(package.DurationDays),
            Status = BookingStatus.Pending,
            TotalPrice = package.Price + package.Activities.Sum(a => a.Price)
        };
    }
}
