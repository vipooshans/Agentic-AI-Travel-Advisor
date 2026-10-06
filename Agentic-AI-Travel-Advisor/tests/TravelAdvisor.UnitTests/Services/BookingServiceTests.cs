using System.Data;
using Moq;
using TravelAdvisor.Core.Common;
using TravelAdvisor.Core.DTOs.Bookings;
using TravelAdvisor.Core.Entities;
using TravelAdvisor.Core.Enums;
using TravelAdvisor.Core.Interfaces.Repositories;
using TravelAdvisor.Infrastructure.Services;
using Xunit;

namespace TravelAdvisor.UnitTests.Services;

/// <summary>
/// BookingService against mocked repositories: the rules are exercised without a database,
/// and the mocks verify that nothing is written when a rule rejects the request.
/// </summary>
public class BookingServiceTests
{
    private static readonly DateTime Now = new(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Today = Now.Date;

    private const string GuestId = "guest-1";
    private const string OwnerId = "owner-1";
    private const string AgentId = "agent-1";

    private readonly Mock<IBookingRepository> _bookings = new();
    private readonly Mock<IRoomRepository> _rooms = new();
    private readonly Mock<IPackageRepository> _packages = new();
    private readonly Mock<IRoomAvailabilityRepository> _availability = new();
    private readonly Mock<ISystemSettingRepository> _settings = new();
    private readonly Mock<IPaymentRepository> _payments = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<TimeProvider> _clock = new();
    private Booking? _added;

    public BookingServiceTests()
    {
        _clock.Setup(c => c.GetUtcNow()).Returns(new DateTimeOffset(Now));
        _unitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task<int>>>(), It.IsAny<IsolationLevel>(), It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task<int>> action, IsolationLevel _, CancellationToken ct) => action(ct));
        _availability
            .Setup(a => a.ListAsync(It.IsAny<int>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _payments.Setup(p => p.ListByBookingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _bookings.Setup(b => b.Add(It.IsAny<Booking>())).Callback<Booking>(b =>
        {
            b.Id = 501;
            _added = b;
        });
        _bookings.Setup(b => b.GetByIdAsync(501, It.IsAny<CancellationToken>())).ReturnsAsync(() => _added);
    }

    private BookingService CreateService() => new(
        _bookings.Object, _rooms.Object, _packages.Object, _availability.Object,
        _settings.Object, _payments.Object, _unitOfWork.Object, _clock.Object);

    private static UserContext Guest => new(GuestId, RoleNames.User);

    private static Room ApprovedRoom(int id = 7, decimal price = 10_000m, int capacity = 2, bool available = true) => new()
    {
        Id = id,
        HotelId = 3,
        Name = "Deluxe",
        PricePerNight = price,
        Capacity = capacity,
        IsAvailable = available,
        Hotel = new Hotel { Id = 3, OwnerId = OwnerId, Name = "Ella Hills", ApprovalStatus = ApprovalStatus.Approved }
    };

    private static TravelPackage ApprovedPackage(int id = 9, decimal price = 20_000m, int maxTravelers = 6) => new()
    {
        Id = id,
        AgentId = AgentId,
        Title = "Ella Explorer",
        Price = price,
        DurationDays = 3,
        MaxTravelers = maxTravelers,
        ApprovalStatus = ApprovalStatus.Approved,
        Activities =
        [
            new PackageActivity { Title = "Nine Arches", Price = 1_500m },
            new PackageActivity { Title = "Little Adam's Peak", Price = 500m }
        ]
    };

    private void GivenRoom(Room room) =>
        _rooms.Setup(r => r.GetByIdAsync(room.Id, It.IsAny<CancellationToken>())).ReturnsAsync(room);

    private void GivenPackage(TravelPackage package, int guestsAlreadyBooked = 0, bool guestHasBooking = false)
    {
        _packages.Setup(p => p.GetByIdAsync(package.Id, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(package);
        _bookings.Setup(b => b.CountPackageGuestsAsync(package.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(guestsAlreadyBooked);
        _bookings.Setup(b => b.HasActivePackageBookingAsync(It.IsAny<string>(), package.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(guestHasBooking);
    }

    private void GivenSetting(string key, string value) =>
        _settings.Setup(s => s.GetAsync(key, It.IsAny<CancellationToken>())).ReturnsAsync(new SystemSetting { Key = key, Value = value });

    private static CreateBookingRequest RoomRequest(int roomId = 7, int inDays = 10, int nights = 2, int guests = 2) => new()
    {
        RoomId = roomId,
        CheckIn = Today.AddDays(inDays),
        CheckOut = Today.AddDays(inDays + nights),
        Guests = guests
    };

    private void VerifyNothingWritten()
    {
        _bookings.Verify(b => b.Add(It.IsAny<Booking>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---------- Create: room ----------

    [Fact]
    public async Task Room_booking_is_created_pending_inside_a_locked_transaction()
    {
        GivenRoom(ApprovedRoom());

        var dto = await CreateService().CreateAsync(Guest, RoomRequest());

        Assert.Equal(501, dto.Id);
        Assert.Equal(BookingStatus.Pending, dto.Status);
        Assert.Equal(GuestId, dto.UserId);
        Assert.Equal(20_000m, dto.TotalPrice);
        Assert.Equal(DateTimeKind.Utc, dto.CheckIn.Kind);
        _bookings.Verify(b => b.LockRoomAsync(7, It.IsAny<CancellationToken>()), Times.Once);
        _bookings.Verify(b => b.Add(It.IsAny<Booking>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Room_total_uses_per_night_price_overrides()
    {
        GivenRoom(ApprovedRoom(price: 10_000m));
        var firstNight = DateOnly.FromDateTime(Today.AddDays(10));
        _availability
            .Setup(a => a.ListAsync(7, It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new RoomAvailability { RoomId = 7, Date = firstNight, PriceOverride = 15_000m }]);

        var dto = await CreateService().CreateAsync(Guest, RoomRequest(nights: 3));

        Assert.Equal(15_000m + 10_000m + 10_000m, dto.TotalPrice);
    }

    [Fact]
    public async Task Notes_are_trimmed_and_blank_notes_are_dropped()
    {
        GivenRoom(ApprovedRoom());
        var request = RoomRequest();
        request.Notes = "  late arrival  ";
        Assert.Equal("late arrival", (await CreateService().CreateAsync(Guest, request)).Notes);

        request.Notes = "   ";
        Assert.Null((await CreateService().CreateAsync(Guest, request)).Notes);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task Exactly_one_of_room_or_package_is_required(bool withRoom, bool withPackage)
    {
        var request = new CreateBookingRequest
        {
            RoomId = withRoom ? 7 : null,
            TravelPackageId = withPackage ? 9 : null,
            CheckIn = Today.AddDays(5),
            CheckOut = Today.AddDays(6)
        };

        await Assert.ThrowsAsync<BusinessRuleException>(() => CreateService().CreateAsync(Guest, request));
        _unitOfWork.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task<int>>>(), It.IsAny<IsolationLevel>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Past_check_in_is_rejected_with_a_field_error()
    {
        GivenRoom(ApprovedRoom());

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => CreateService().CreateAsync(Guest, RoomRequest(inDays: -1)));

        Assert.Equal(400, ex.StatusCode);
        Assert.Contains("checkIn", ex.Errors!.Keys);
        VerifyNothingWritten();
    }

    [Fact]
    public async Task Check_in_today_is_allowed_boundary()
    {
        GivenRoom(ApprovedRoom());
        var dto = await CreateService().CreateAsync(Guest, RoomRequest(inDays: 0, nights: 1));
        Assert.Equal(Today, dto.CheckIn);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public async Task Check_out_must_be_after_check_in(int nights)
    {
        GivenRoom(ApprovedRoom());
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => CreateService().CreateAsync(Guest, RoomRequest(nights: nights)));
        Assert.Contains("checkOut", ex.Errors!.Keys);
        VerifyNothingWritten();
    }

    [Fact]
    public async Task Missing_check_out_for_a_room_is_rejected()
    {
        GivenRoom(ApprovedRoom());
        var request = RoomRequest();
        request.CheckOut = null;
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => CreateService().CreateAsync(Guest, request));
        Assert.Contains("checkOut", ex.Errors!.Keys);
    }

    [Theory]
    [InlineData(BookingService.MaxNights, true)]
    [InlineData(BookingService.MaxNights + 1, false)]
    public async Task Stay_length_is_capped_at_max_nights(int nights, bool allowed)
    {
        GivenRoom(ApprovedRoom());
        if (allowed)
        {
            var dto = await CreateService().CreateAsync(Guest, RoomRequest(nights: nights));
            Assert.Equal(nights * 10_000m, dto.TotalPrice);
        }
        else
        {
            await Assert.ThrowsAsync<BusinessRuleException>(() => CreateService().CreateAsync(Guest, RoomRequest(nights: nights)));
            VerifyNothingWritten();
        }
    }

    [Theory]
    [InlineData(10, true)]
    [InlineData(11, false)]
    public async Task Max_advance_days_comes_from_system_settings(int inDays, bool allowed)
    {
        GivenRoom(ApprovedRoom());
        GivenSetting(SystemSettingKeys.MaxAdvanceBookingDays, "10");

        if (allowed)
            Assert.Equal(BookingStatus.Pending, (await CreateService().CreateAsync(Guest, RoomRequest(inDays: inDays))).Status);
        else
            Assert.Contains("10 days", (await Assert.ThrowsAsync<BusinessRuleException>(() => CreateService().CreateAsync(Guest, RoomRequest(inDays: inDays)))).Message);
    }

    [Fact]
    public async Task Invalid_setting_value_falls_back_to_the_default_window()
    {
        GivenRoom(ApprovedRoom());
        GivenSetting(SystemSettingKeys.MaxAdvanceBookingDays, "not-a-number");

        await CreateService().CreateAsync(Guest, RoomRequest(inDays: BookingService.DefaultMaxAdvanceDays));
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            CreateService().CreateAsync(Guest, RoomRequest(inDays: BookingService.DefaultMaxAdvanceDays + 1)));
    }

    [Fact]
    public async Task Unknown_room_is_not_found()
    {
        var ex = await Assert.ThrowsAsync<NotFoundException>(() => CreateService().CreateAsync(Guest, RoomRequest(roomId: 999)));
        Assert.Equal(404, ex.StatusCode);
        VerifyNothingWritten();
    }

    [Theory]
    [InlineData(ApprovalStatus.Pending)]
    [InlineData(ApprovalStatus.Rejected)]
    public async Task Rooms_in_unapproved_hotels_cannot_be_booked(ApprovalStatus status)
    {
        var room = ApprovedRoom();
        room.Hotel.ApprovalStatus = status;
        GivenRoom(room);

        await Assert.ThrowsAsync<BusinessRuleException>(() => CreateService().CreateAsync(Guest, RoomRequest()));
        VerifyNothingWritten();
    }

    [Fact]
    public async Task Room_marked_unavailable_is_a_conflict()
    {
        GivenRoom(ApprovedRoom(available: false));
        var ex = await Assert.ThrowsAsync<ConflictException>(() => CreateService().CreateAsync(Guest, RoomRequest()));
        Assert.Equal(409, ex.StatusCode);
        VerifyNothingWritten();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public async Task Guest_count_must_fit_room_capacity(int guests)
    {
        GivenRoom(ApprovedRoom(capacity: 2));
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => CreateService().CreateAsync(Guest, RoomRequest(guests: guests)));
        Assert.Contains("guests", ex.Errors!.Keys);
        VerifyNothingWritten();
    }

    [Fact]
    public async Task Blocked_night_is_a_conflict_that_names_the_date()
    {
        GivenRoom(ApprovedRoom());
        var blocked = DateOnly.FromDateTime(Today.AddDays(11));
        _availability
            .Setup(a => a.ListAsync(7, It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new RoomAvailability { RoomId = 7, Date = blocked, IsBlocked = true }]);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => CreateService().CreateAsync(Guest, RoomRequest()));

        Assert.Contains(blocked.ToString("yyyy-MM-dd"), ex.Message);
        VerifyNothingWritten();
    }

    [Fact]
    public async Task Overlapping_booking_is_a_conflict_and_nothing_is_saved()
    {
        GivenRoom(ApprovedRoom());
        _bookings.Setup(b => b.HasRoomOverlapAsync(7, It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() => CreateService().CreateAsync(Guest, RoomRequest()));
        VerifyNothingWritten();
    }

    // ---------- Create: package ----------

    [Fact]
    public async Task Package_price_includes_activities_and_is_per_traveler()
    {
        GivenPackage(ApprovedPackage(price: 20_000m));
        var request = new CreateBookingRequest { TravelPackageId = 9, CheckIn = Today.AddDays(14), Guests = 2 };

        var dto = await CreateService().CreateAsync(Guest, request);

        Assert.Equal((20_000m + 1_500m + 500m) * 2, dto.TotalPrice);
        Assert.Equal(Today.AddDays(14 + 3), dto.CheckOut);
        _bookings.Verify(b => b.LockPackageAsync(9, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Package_seats_left_are_enforced()
    {
        GivenPackage(ApprovedPackage(maxTravelers: 6), guestsAlreadyBooked: 5);
        var request = new CreateBookingRequest { TravelPackageId = 9, CheckIn = Today.AddDays(14), Guests = 2 };

        var ex = await Assert.ThrowsAsync<ConflictException>(() => CreateService().CreateAsync(Guest, request));

        Assert.Contains("Only 1 place(s) left", ex.Message);
        VerifyNothingWritten();
    }

    [Fact]
    public async Task Fully_booked_package_says_so()
    {
        GivenPackage(ApprovedPackage(maxTravelers: 6), guestsAlreadyBooked: 6);
        var request = new CreateBookingRequest { TravelPackageId = 9, CheckIn = Today.AddDays(14), Guests = 1 };

        var ex = await Assert.ThrowsAsync<ConflictException>(() => CreateService().CreateAsync(Guest, request));

        Assert.Contains("fully booked", ex.Message);
    }

    [Fact]
    public async Task Duplicate_package_booking_on_the_same_date_is_refused()
    {
        GivenPackage(ApprovedPackage(), guestHasBooking: true);
        var request = new CreateBookingRequest { TravelPackageId = 9, CheckIn = Today.AddDays(14), Guests = 1 };

        var ex = await Assert.ThrowsAsync<ConflictException>(() => CreateService().CreateAsync(Guest, request));

        Assert.Contains("already have a booking", ex.Message);
        VerifyNothingWritten();
    }

    [Fact]
    public async Task Unapproved_or_missing_package_cannot_be_booked()
    {
        var pending = ApprovedPackage();
        pending.ApprovalStatus = ApprovalStatus.Pending;
        GivenPackage(pending);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            CreateService().CreateAsync(Guest, new CreateBookingRequest { TravelPackageId = 9, CheckIn = Today.AddDays(3) }));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateService().CreateAsync(Guest, new CreateBookingRequest { TravelPackageId = 404, CheckIn = Today.AddDays(3) }));
        VerifyNothingWritten();
    }

    // ---------- Availability ----------

    [Fact]
    public async Task Availability_reports_rule_failures_instead_of_throwing()
    {
        GivenRoom(ApprovedRoom());
        _bookings.Setup(b => b.HasRoomOverlapAsync(7, It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var quote = await CreateService().CheckAvailabilityAsync(null, new AvailabilityQuery
        {
            RoomId = 7, CheckIn = Today.AddDays(10), CheckOut = Today.AddDays(12), Guests = 2
        });

        Assert.False(quote.Available);
        Assert.Contains("already booked", quote.Reason);
        Assert.Equal(0m, quote.TotalPrice);
        VerifyNothingWritten();
    }

    [Fact]
    public async Task Availability_quotes_the_price_and_remaining_places()
    {
        GivenPackage(ApprovedPackage(maxTravelers: 6), guestsAlreadyBooked: 2);

        var quote = await CreateService().CheckAvailabilityAsync(null, new AvailabilityQuery
        {
            TravelPackageId = 9, CheckIn = Today.AddDays(20), Guests = 3
        });

        Assert.True(quote.Available);
        Assert.Equal(1, quote.RemainingPlaces);
        Assert.Equal(3, quote.Nights);
        Assert.Equal(22_000m * 3, quote.TotalPrice);
        _bookings.Verify(b => b.HasActivePackageBookingAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Never, "anonymous quotes cannot check for the caller's own duplicate booking");
    }

    [Fact]
    public async Task Availability_still_throws_for_unknown_rooms()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => CreateService().CheckAvailabilityAsync(Guest, new AvailabilityQuery
        {
            RoomId = 999, CheckIn = Today.AddDays(10), CheckOut = Today.AddDays(11)
        }));
    }

    // ---------- Listing and access ----------

    [Theory]
    [InlineData(RoleNames.Admin, null, null, null)]
    [InlineData(RoleNames.HotelOwner, null, "u", null)]
    [InlineData(RoleNames.TravelAgent, null, null, "u")]
    [InlineData(RoleNames.User, "u", null, null)]
    public async Task List_is_scoped_to_the_callers_role(string role, string? guest, string? owner, string? agent)
    {
        _bookings.Setup(b => b.ListAsync(It.IsAny<BookingFilter>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);

        await CreateService().ListAsync(new UserContext("u", role));

        _bookings.Verify(b => b.ListAsync(
            It.Is<BookingFilter>(f => f.GuestUserId == guest && f.HotelOwnerId == owner && f.AgentId == agent),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private Booking GivenBooking(BookingStatus status, int checkInDays = 10, string userId = GuestId, bool package = false)
    {
        var booking = new Booking
        {
            Id = 77,
            UserId = userId,
            Status = status,
            CheckIn = Today.AddDays(checkInDays),
            CheckOut = Today.AddDays(checkInDays + 2),
            Room = package ? null : ApprovedRoom(),
            RoomId = package ? null : 7,
            TravelPackage = package ? ApprovedPackage() : null,
            TravelPackageId = package ? 9 : null
        };
        _bookings.Setup(b => b.GetByIdAsync(77, It.IsAny<CancellationToken>())).ReturnsAsync(booking);
        return booking;
    }

    [Theory]
    [InlineData("someone-else", RoleNames.User)]
    [InlineData("other-owner", RoleNames.HotelOwner)]
    [InlineData("other-agent", RoleNames.TravelAgent)]
    public async Task Get_refuses_callers_who_do_not_own_the_booking(string userId, string role)
    {
        GivenBooking(BookingStatus.Pending);
        await Assert.ThrowsAsync<ForbiddenException>(() => CreateService().GetAsync(new UserContext(userId, role), 77));
    }

    [Theory]
    [InlineData(GuestId, RoleNames.User)]
    [InlineData(OwnerId, RoleNames.HotelOwner)]
    [InlineData("any-admin", RoleNames.Admin)]
    public async Task Get_allows_the_guest_the_hotel_owner_and_admins(string userId, string role)
    {
        GivenBooking(BookingStatus.Pending);
        Assert.Equal(77, (await CreateService().GetAsync(new UserContext(userId, role), 77)).Id);
    }

    [Fact]
    public async Task Agent_can_only_see_bookings_of_their_own_packages()
    {
        GivenBooking(BookingStatus.Pending, package: true);
        Assert.Equal(77, (await CreateService().GetAsync(new UserContext(AgentId, RoleNames.TravelAgent), 77)).Id);
        await Assert.ThrowsAsync<ForbiddenException>(() => CreateService().GetAsync(new UserContext(OwnerId, RoleNames.HotelOwner), 77));
    }

    // ---------- Status changes ----------

    private Task<BookingDto> ChangeStatus(UserContext caller, BookingStatus status) =>
        CreateService().UpdateStatusAsync(caller, 77, new UpdateBookingStatusRequest { Status = status });

    [Fact]
    public async Task Owner_confirms_a_pending_booking()
    {
        GivenBooking(BookingStatus.Pending);
        var dto = await ChangeStatus(new UserContext(OwnerId, RoleNames.HotelOwner), BookingStatus.Confirmed);
        Assert.Equal(BookingStatus.Confirmed, dto.Status);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Another_owner_cannot_change_the_status()
    {
        var booking = GivenBooking(BookingStatus.Pending);
        await Assert.ThrowsAsync<ForbiddenException>(() => ChangeStatus(new UserContext("other-owner", RoleNames.HotelOwner), BookingStatus.Confirmed));
        Assert.Equal(BookingStatus.Pending, booking.Status);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Guest_cannot_confirm_their_own_booking()
    {
        GivenBooking(BookingStatus.Pending);
        await Assert.ThrowsAsync<BusinessRuleException>(() => ChangeStatus(Guest, BookingStatus.Confirmed));
    }

    [Fact]
    public async Task Setting_the_same_status_is_rejected()
    {
        GivenBooking(BookingStatus.Pending);
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => ChangeStatus(Guest, BookingStatus.Pending));
        Assert.Contains("already Pending", ex.Message);
    }

    [Fact]
    public async Task Unknown_booking_is_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateService().UpdateStatusAsync(Guest, 12345, new UpdateBookingStatusRequest { Status = BookingStatus.Cancelled }));
    }

    [Fact]
    public async Task Cancelling_refunds_completed_payments_and_voids_pending_ones()
    {
        GivenBooking(BookingStatus.Pending);
        var paid = new Payment { Id = 1, BookingId = 77, Status = PaymentStatus.Completed };
        var waiting = new Payment { Id = 2, BookingId = 77, Status = PaymentStatus.Pending };
        var failed = new Payment { Id = 3, BookingId = 77, Status = PaymentStatus.Failed };
        _payments.Setup(p => p.ListByBookingAsync(77, It.IsAny<CancellationToken>())).ReturnsAsync([paid, waiting, failed]);

        var dto = await ChangeStatus(Guest, BookingStatus.Cancelled);

        Assert.Equal(BookingStatus.Cancelled, dto.Status);
        Assert.Equal(Now, dto.CancelledAt);
        Assert.Equal(PaymentStatus.Refunded, paid.Status);
        Assert.Equal(PaymentStatus.Failed, waiting.Status);
        Assert.Equal(PaymentStatus.Failed, failed.Status);
    }

    [Fact]
    public async Task Guest_cannot_cancel_a_confirmed_booking_inside_the_cutoff()
    {
        GivenSetting(SystemSettingKeys.GuestCancellationCutoffHours, "48");
        var booking = GivenBooking(BookingStatus.Confirmed, checkInDays: 1);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => ChangeStatus(Guest, BookingStatus.Cancelled));

        Assert.Contains("48 hours", ex.Message);
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
    }

    [Fact]
    public async Task Guest_can_cancel_a_confirmed_booking_before_the_cutoff()
    {
        GivenBooking(BookingStatus.Confirmed, checkInDays: 5);
        Assert.Equal(BookingStatus.Cancelled, (await ChangeStatus(Guest, BookingStatus.Cancelled)).Status);
    }

    [Fact]
    public async Task Booking_cannot_be_completed_before_check_in()
    {
        GivenBooking(BookingStatus.Confirmed, checkInDays: 3);
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => ChangeStatus(new UserContext(OwnerId, RoleNames.HotelOwner), BookingStatus.Completed));
        Assert.Contains("on or after its check-in", ex.Message);
    }

    [Fact]
    public async Task Booking_can_be_completed_on_the_check_in_day()
    {
        GivenBooking(BookingStatus.Confirmed, checkInDays: 0);
        Assert.Equal(BookingStatus.Completed, (await ChangeStatus(new UserContext(OwnerId, RoleNames.HotelOwner), BookingStatus.Completed)).Status);
    }

    [Theory]
    [InlineData(BookingStatus.Cancelled, BookingStatus.Confirmed)]
    [InlineData(BookingStatus.Completed, BookingStatus.Cancelled)]
    [InlineData(BookingStatus.Pending, BookingStatus.Completed)]
    public async Task Terminal_and_skipped_transitions_are_refused_even_for_admins(BookingStatus from, BookingStatus to)
    {
        GivenBooking(from, checkInDays: -1);
        await Assert.ThrowsAsync<BusinessRuleException>(() => ChangeStatus(new UserContext("admin", RoleNames.Admin), to));
    }
}
