using Moq;
using TravelAdvisor.Core.Common;
using TravelAdvisor.Core.DTOs.Bookings;
using TravelAdvisor.Core.DTOs.Destinations;
using TravelAdvisor.Core.DTOs.Hotels;
using TravelAdvisor.Core.DTOs.Packages;
using TravelAdvisor.Core.Entities;
using TravelAdvisor.Core.Enums;
using TravelAdvisor.Core.Interfaces.Repositories;
using TravelAdvisor.Infrastructure.Services;
using Xunit;

namespace TravelAdvisor.UnitTests.Services;

/// <summary>Hotel, room, package and destination services: ownership, visibility and re-approval rules.</summary>
public class CatalogServiceTests
{
    private const string OwnerId = "owner-1";
    private const string AgentId = "agent-1";

    private static readonly UserContext Owner = new(OwnerId, RoleNames.HotelOwner);
    private static readonly UserContext OtherOwner = new("owner-2", RoleNames.HotelOwner);
    private static readonly UserContext Agent = new(AgentId, RoleNames.TravelAgent);
    private static readonly UserContext OtherAgent = new("agent-2", RoleNames.TravelAgent);
    private static readonly UserContext Admin = new("admin-1", RoleNames.Admin);
    private static readonly UserContext Traveler = new("user-1", RoleNames.User);

    private readonly Mock<IHotelRepository> _hotels = new();
    private readonly Mock<IRoomRepository> _rooms = new();
    private readonly Mock<IRoomAvailabilityRepository> _availability = new();
    private readonly Mock<IBookingRepository> _bookings = new();
    private readonly Mock<IPackageRepository> _packages = new();
    private readonly Mock<IDestinationRepository> _destinations = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public CatalogServiceTests()
    {
        _hotels.Setup(h => h.SearchAsync(It.IsAny<HotelSearchCriteria>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _packages.Setup(p => p.SearchAsync(It.IsAny<PackageSearchCriteria>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _availability.Setup(a => a.ListAsync(It.IsAny<int>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _bookings.Setup(b => b.GetBookedNightsAsync(It.IsAny<int>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
    }

    private HotelService Hotels() => new(_hotels.Object, _unitOfWork.Object);
    private RoomService Rooms() => new(_hotels.Object, _rooms.Object, _availability.Object, _bookings.Object, _unitOfWork.Object);
    private PackageService Packages() => new(_packages.Object, _destinations.Object, _unitOfWork.Object);
    private DestinationService Destinations() => new(_destinations.Object, _unitOfWork.Object);

    private void VerifyNotSaved() => _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

    private Hotel GivenHotel(ApprovalStatus status = ApprovalStatus.Approved)
    {
        var hotel = new Hotel
        {
            Id = 3, OwnerId = OwnerId, Name = "Ella Hills", Address = "1 Main St", City = "Ella", Country = "Sri Lanka",
            ApprovalStatus = status, Rooms = [new Room { Id = 7, HotelId = 3, Name = "Deluxe", PricePerNight = 12_000m, Capacity = 2 }]
        };
        _hotels.Setup(h => h.GetByIdAsync(3, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(hotel);
        return hotel;
    }

    private static UpdateHotelRequest SameAs(Hotel h) => new()
    {
        Name = h.Name, Address = h.Address, City = h.City, Country = h.Country, Description = h.Description, ImageUrl = h.ImageUrl
    };

    // ---------- Hotels ----------

    [Fact]
    public async Task Public_hotel_search_only_returns_approved_listings_even_if_a_status_is_requested()
    {
        await Hotels().SearchAsync(null, new HotelSearchQuery { Q = "ella", ApprovalStatus = ApprovalStatus.Pending, MaxPrice = 20_000m });

        _hotels.Verify(h => h.SearchAsync(
            It.Is<HotelSearchCriteria>(c => c.ApprovedOnly && c.ApprovalStatus == null && c.Query == "ella" && c.MaxPricePerNight == 20_000m),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Admin_hotel_search_can_filter_by_approval_status()
    {
        await Hotels().SearchAsync(Admin, new HotelSearchQuery { ApprovalStatus = ApprovalStatus.Pending });

        _hotels.Verify(h => h.SearchAsync(
            It.Is<HotelSearchCriteria>(c => !c.ApprovedOnly && c.ApprovalStatus == ApprovalStatus.Pending),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(ApprovalStatus.Pending)]
    [InlineData(ApprovalStatus.Rejected)]
    public async Task Unapproved_hotel_is_hidden_from_travelers_and_other_owners(ApprovalStatus status)
    {
        GivenHotel(status);

        await Assert.ThrowsAsync<NotFoundException>(() => Hotels().GetAsync(null, 3));
        await Assert.ThrowsAsync<NotFoundException>(() => Hotels().GetAsync(Traveler, 3));
        await Assert.ThrowsAsync<NotFoundException>(() => Hotels().GetAsync(OtherOwner, 3));
        Assert.Equal(3, (await Hotels().GetAsync(Owner, 3)).Id);
        Assert.Equal(3, (await Hotels().GetAsync(Admin, 3)).Id);
    }

    [Fact]
    public async Task Hotel_detail_includes_rooms_and_minimum_price()
    {
        GivenHotel();
        var dto = await Hotels().GetAsync(null, 3);
        Assert.Single(dto.Rooms);
        Assert.Equal(12_000m, dto.MinPricePerNight);
    }

    [Fact]
    public async Task New_hotel_belongs_to_the_caller_and_starts_pending()
    {
        Hotel? added = null;
        _hotels.Setup(h => h.Add(It.IsAny<Hotel>())).Callback<Hotel>(h => added = h);

        var dto = await Hotels().CreateAsync(Owner, new CreateHotelRequest
        {
            Name = "  Cliff View ", Address = "2 Hill Rd", City = "Ella", Country = "Sri Lanka", Description = "  "
        });

        Assert.Equal(OwnerId, added!.OwnerId);
        Assert.Equal(ApprovalStatus.Pending, dto.ApprovalStatus);
        Assert.Equal("Cliff View", dto.Name);
        Assert.Null(dto.Description);
    }

    [Fact]
    public async Task Another_owner_cannot_update_or_delete_the_hotel()
    {
        var hotel = GivenHotel();

        await Assert.ThrowsAsync<ForbiddenException>(() => Hotels().UpdateAsync(OtherOwner, 3, SameAs(hotel) ));
        await Assert.ThrowsAsync<ForbiddenException>(() => Hotels().DeleteAsync(OtherOwner, 3));
        _hotels.Verify(h => h.Remove(It.IsAny<Hotel>()), Times.Never);
        VerifyNotSaved();
    }

    [Fact]
    public async Task Owner_edit_sends_an_approved_hotel_back_for_review()
    {
        var hotel = GivenHotel();
        var request = SameAs(hotel);
        request.Name = "Ella Hills Resort";

        var dto = await Hotels().UpdateAsync(Owner, 3, request);

        Assert.Equal(ApprovalStatus.Pending, dto.ApprovalStatus);
    }

    [Fact]
    public async Task Saving_without_changes_keeps_the_approval()
    {
        var hotel = GivenHotel();
        Assert.Equal(ApprovalStatus.Approved, (await Hotels().UpdateAsync(Owner, 3, SameAs(hotel))).ApprovalStatus);
    }

    [Fact]
    public async Task Admin_edit_keeps_the_approval()
    {
        var hotel = GivenHotel();
        var request = SameAs(hotel);
        request.Description = "Fixed a typo";
        Assert.Equal(ApprovalStatus.Approved, (await Hotels().UpdateAsync(Admin, 3, request)).ApprovalStatus);
    }

    [Fact]
    public async Task Hotel_with_bookings_cannot_be_deleted()
    {
        GivenHotel();
        _hotels.Setup(h => h.HasBookingsAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() => Hotels().DeleteAsync(Owner, 3));
        _hotels.Verify(h => h.Remove(It.IsAny<Hotel>()), Times.Never);
    }

    [Fact]
    public async Task Approval_decision_must_be_approved_or_rejected()
    {
        GivenHotel(ApprovalStatus.Pending);

        await Assert.ThrowsAsync<BusinessRuleException>(() => Hotels().SetApprovalAsync(3, new UpdateApprovalRequest { Status = ApprovalStatus.Pending }));
        Assert.Equal(ApprovalStatus.Rejected, (await Hotels().SetApprovalAsync(3, new UpdateApprovalRequest { Status = ApprovalStatus.Rejected })).ApprovalStatus);
        await Assert.ThrowsAsync<NotFoundException>(() => Hotels().SetApprovalAsync(404, new UpdateApprovalRequest { Status = ApprovalStatus.Approved }));
    }

    // ---------- Rooms ----------

    private Room GivenRoom()
    {
        var room = new Room { Id = 7, HotelId = 3, Name = "Deluxe", RoomType = "Double", PricePerNight = 12_000m, Capacity = 2, Hotel = new Hotel { Id = 3, OwnerId = OwnerId } };
        _rooms.Setup(r => r.GetAsync(3, 7, It.IsAny<CancellationToken>())).ReturnsAsync(room);
        return room;
    }

    [Fact]
    public async Task Another_owner_cannot_change_room_prices_or_availability()
    {
        var room = GivenRoom();

        await Assert.ThrowsAsync<ForbiddenException>(() => Rooms().UpdateAsync(OtherOwner, 3, 7, new UpdateRoomRequest { Name = "X", RoomType = "Y", PricePerNight = 1m, Capacity = 1 }));
        await Assert.ThrowsAsync<ForbiddenException>(() => Rooms().SetAvailabilityAsync(OtherOwner, 3, 7, false));
        await Assert.ThrowsAsync<ForbiddenException>(() => Rooms().DeleteAsync(OtherOwner, 3, 7));

        Assert.Equal(12_000m, room.PricePerNight);
        Assert.True(room.IsAvailable);
        VerifyNotSaved();
    }

    [Fact]
    public async Task Rooms_cannot_be_added_to_someone_elses_hotel()
    {
        GivenHotel();
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            Rooms().CreateAsync(OtherOwner, 3, new CreateRoomRequest { Name = "Suite", RoomType = "Suite", PricePerNight = 30_000m, Capacity = 3 }));
        _rooms.Verify(r => r.Add(It.IsAny<Room>()), Times.Never);
    }

    [Fact]
    public async Task Blocking_a_booked_night_is_refused()
    {
        GivenRoom();
        var night = new DateOnly(2026, 11, 5);
        _bookings.Setup(b => b.GetBookedNightsAsync(7, It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>())).ReturnsAsync([night]);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => Rooms().SaveCalendarAsync(Owner, 3, 7, new SaveRoomCalendarRequest
        {
            Entries = [new RoomCalendarEntryDto { Date = night, IsBlocked = true }]
        }));

        Assert.Contains("2026-11-05", ex.Message);
        VerifyNotSaved();
    }

    [Fact]
    public async Task Calendar_rejects_duplicate_dates_and_empty_requests()
    {
        GivenRoom();
        var day = new DateOnly(2026, 11, 5);

        await Assert.ThrowsAsync<BusinessRuleException>(() => Rooms().SaveCalendarAsync(Owner, 3, 7, new SaveRoomCalendarRequest()));
        await Assert.ThrowsAsync<BusinessRuleException>(() => Rooms().SaveCalendarAsync(Owner, 3, 7, new SaveRoomCalendarRequest
        {
            Entries = [new RoomCalendarEntryDto { Date = day, IsBlocked = true }, new RoomCalendarEntryDto { Date = day, PriceOverride = 9_000m }]
        }));
    }

    [Fact]
    public async Task Calendar_entry_without_block_or_price_removes_the_override()
    {
        GivenRoom();
        var day = new DateOnly(2026, 11, 5);
        var existing = new RoomAvailability { RoomId = 7, Date = day, PriceOverride = 9_000m };
        _availability.Setup(a => a.ListAsync(7, It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>())).ReturnsAsync([existing]);

        await Rooms().SaveCalendarAsync(Owner, 3, 7, new SaveRoomCalendarRequest { Entries = [new RoomCalendarEntryDto { Date = day }] });

        _availability.Verify(a => a.Remove(existing), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(RoomService.MaxCalendarDays + 1)]
    public async Task Calendar_range_must_be_positive_and_bounded(int days)
    {
        var from = new DateOnly(2026, 11, 1);
        await Assert.ThrowsAsync<BusinessRuleException>(() => Rooms().GetCalendarAsync(Owner, 3, 7, from, from.AddDays(days)));
    }

    [Fact]
    public async Task Rooms_of_an_unapproved_hotel_are_hidden_from_the_public()
    {
        GivenHotel(ApprovalStatus.Pending);
        await Assert.ThrowsAsync<NotFoundException>(() => Rooms().ListAsync(null, 3));
    }

    // ---------- Packages ----------

    private TravelPackage GivenPackage(ApprovalStatus status = ApprovalStatus.Approved)
    {
        var package = new TravelPackage
        {
            Id = 9, AgentId = AgentId, DestinationId = 1, Destination = new Destination { Id = 1, Name = "Ella", Country = "Sri Lanka" },
            Title = "Ella Explorer", Price = 20_000m, DurationDays = 3, MaxTravelers = 6, ApprovalStatus = status,
            Activities = [new PackageActivity { Id = 1, Title = "Hike", Price = 2_000m, DayNumber = 1 }]
        };
        _packages.Setup(p => p.GetByIdAsync(9, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(package);
        return package;
    }

    private static UpdatePackageRequest SameAs(TravelPackage p) => new()
    {
        DestinationId = p.DestinationId, Title = p.Title, Description = p.Description, Price = p.Price,
        DurationDays = p.DurationDays, ImageUrl = p.ImageUrl, MaxTravelers = p.MaxTravelers
    };

    [Fact]
    public async Task Package_total_price_includes_activities()
    {
        GivenPackage();
        Assert.Equal(22_000m, (await Packages().GetAsync(null, 9)).TotalPrice);
    }

    [Fact]
    public async Task Pending_package_is_only_visible_to_its_agent_and_admins()
    {
        GivenPackage(ApprovalStatus.Pending);

        await Assert.ThrowsAsync<NotFoundException>(() => Packages().GetAsync(Traveler, 9));
        await Assert.ThrowsAsync<NotFoundException>(() => Packages().GetAsync(OtherAgent, 9));
        Assert.Equal(9, (await Packages().GetAsync(Agent, 9)).Id);
        Assert.Equal(9, (await Packages().GetAsync(Admin, 9)).Id);
    }

    [Fact]
    public async Task Another_agent_cannot_edit_delete_or_add_activities()
    {
        var package = GivenPackage();

        await Assert.ThrowsAsync<ForbiddenException>(() => Packages().UpdateAsync(OtherAgent, 9, SameAs(package)));
        await Assert.ThrowsAsync<ForbiddenException>(() => Packages().DeleteAsync(OtherAgent, 9));
        await Assert.ThrowsAsync<ForbiddenException>(() => Packages().AddActivityAsync(OtherAgent, 9, new CreateActivityRequest { Title = "X", DayNumber = 1 }));
        _packages.Verify(p => p.Remove(It.IsAny<TravelPackage>()), Times.Never);
        _packages.Verify(p => p.AddActivity(It.IsAny<PackageActivity>()), Times.Never);
        VerifyNotSaved();
    }

    [Fact]
    public async Task Price_change_sends_the_package_back_for_review()
    {
        var package = GivenPackage();
        var request = SameAs(package);
        request.Price = 18_000m;

        Assert.Equal(ApprovalStatus.Pending, (await Packages().UpdateAsync(Agent, 9, request)).ApprovalStatus);
    }

    [Fact]
    public async Task Moving_a_package_to_an_unknown_destination_is_rejected()
    {
        var package = GivenPackage();
        var request = SameAs(package);
        request.DestinationId = 404;

        await Assert.ThrowsAsync<BusinessRuleException>(() => Packages().UpdateAsync(Agent, 9, request));
        VerifyNotSaved();
    }

    [Fact]
    public async Task Creating_a_package_needs_an_existing_destination()
    {
        await Assert.ThrowsAsync<BusinessRuleException>(() => Packages().CreateAsync(Agent, new CreatePackageRequest
        {
            DestinationId = 404, Title = "Nowhere", Price = 1m, DurationDays = 1
        }));
        _packages.Verify(p => p.Add(It.IsAny<TravelPackage>()), Times.Never);
    }

    [Theory]
    [InlineData(3, true)]
    [InlineData(4, false)]
    public async Task Activity_day_must_fall_within_the_package_duration(int day, bool allowed)
    {
        GivenPackage();
        var request = new CreateActivityRequest { Title = "Tea factory", DayNumber = day, Price = 1_000m };

        if (allowed)
        {
            Assert.Equal(day, (await Packages().AddActivityAsync(Agent, 9, request)).DayNumber);
        }
        else
        {
            var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => Packages().AddActivityAsync(Agent, 9, request));
            Assert.Contains("dayNumber", ex.Errors!.Keys);
        }
    }

    [Fact]
    public async Task Adding_an_activity_requires_re_approval()
    {
        var package = GivenPackage();
        await Packages().AddActivityAsync(Agent, 9, new CreateActivityRequest { Title = "Tea factory", DayNumber = 2 });
        Assert.Equal(ApprovalStatus.Pending, package.ApprovalStatus);
    }

    [Fact]
    public async Task Package_with_bookings_cannot_be_deleted()
    {
        GivenPackage();
        _packages.Setup(p => p.HasBookingsAsync(9, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        await Assert.ThrowsAsync<ConflictException>(() => Packages().DeleteAsync(Agent, 9));
    }

    [Fact]
    public async Task Public_package_search_ignores_requested_approval_status()
    {
        await Packages().SearchAsync(Traveler, new PackageSearchQuery { ApprovalStatus = ApprovalStatus.Rejected, MaxPrice = 50_000m });

        _packages.Verify(p => p.SearchAsync(
            It.Is<PackageSearchCriteria>(c => c.ApprovedOnly && c.ApprovalStatus == null && c.MaxPrice == 50_000m),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- Destinations ----------

    [Fact]
    public async Task Duplicate_destination_name_in_the_same_country_is_a_conflict()
    {
        _destinations.Setup(d => d.NameExistsAsync("Ella", "Sri Lanka", null, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() => Destinations().CreateAsync(new SaveDestinationRequest { Name = "Ella", Country = "Sri Lanka" }));
        _destinations.Verify(d => d.Add(It.IsAny<Destination>()), Times.Never);
    }

    [Fact]
    public async Task Destination_with_packages_cannot_be_deleted()
    {
        _destinations.Setup(d => d.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new Destination { Id = 1, Name = "Ella" });
        _destinations.Setup(d => d.CountPackagesAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(2);

        await Assert.ThrowsAsync<ConflictException>(() => Destinations().DeleteAsync(1));
        _destinations.Verify(d => d.Remove(It.IsAny<Destination>()), Times.Never);
    }

    [Fact]
    public async Task Unknown_destination_is_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => Destinations().GetAsync(404));
        await Assert.ThrowsAsync<NotFoundException>(() => Destinations().UpdateAsync(404, new SaveDestinationRequest { Name = "X", Country = "Y" }));
    }
}
