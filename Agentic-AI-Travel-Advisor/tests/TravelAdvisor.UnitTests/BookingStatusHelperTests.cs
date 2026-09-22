using TravelAdvisor.Core.Entities;
using TravelAdvisor.Core.Enums;
using TravelAdvisor.Infrastructure.Helpers;
using Xunit;

namespace TravelAdvisor.UnitTests;

public class BookingStatusHelperTests
{
    [Theory]
    [InlineData(RoleNames.Admin, false, true)]
    [InlineData(RoleNames.HotelOwner, false, true)]
    [InlineData(RoleNames.TravelAgent, false, true)]
    [InlineData(RoleNames.User, true, false)]
    [InlineData(RoleNames.User, false, false)]
    public void Pending_to_Confirmed(string role, bool isGuest, bool expected)
    {
        Assert.Equal(expected, BookingStatusHelper.CanTransition(BookingStatus.Pending, BookingStatus.Confirmed, role, isGuest));
    }

    [Theory]
    [InlineData(RoleNames.User, true, true)]
    [InlineData(RoleNames.Admin, false, true)]
    [InlineData(RoleNames.HotelOwner, false, true)]
    [InlineData(RoleNames.TravelAgent, false, true)]
    [InlineData(RoleNames.User, false, false)]
    public void Pending_to_Cancelled(string role, bool isGuest, bool expected)
    {
        Assert.Equal(expected, BookingStatusHelper.CanTransition(BookingStatus.Pending, BookingStatus.Cancelled, role, isGuest));
    }

    [Theory]
    [InlineData(BookingStatus.Completed, RoleNames.Admin, false, true)]
    [InlineData(BookingStatus.Cancelled, RoleNames.HotelOwner, false, true)]
    [InlineData(BookingStatus.Completed, RoleNames.TravelAgent, false, true)]
    [InlineData(BookingStatus.Completed, RoleNames.User, true, false)]
    [InlineData(BookingStatus.Cancelled, RoleNames.User, true, false)]
    public void Confirmed_to_Completed_or_Cancelled(BookingStatus to, string role, bool isGuest, bool expected)
    {
        Assert.Equal(expected, BookingStatusHelper.CanTransition(BookingStatus.Confirmed, to, role, isGuest));
    }

    [Fact]
    public void Illegal_jumps_are_rejected()
    {
        Assert.False(BookingStatusHelper.CanTransition(BookingStatus.Completed, BookingStatus.Pending, RoleNames.Admin, false));
        Assert.False(BookingStatusHelper.CanTransition(BookingStatus.Cancelled, BookingStatus.Confirmed, RoleNames.Admin, false));
        Assert.False(BookingStatusHelper.CanTransition(BookingStatus.Completed, BookingStatus.Cancelled, RoleNames.Admin, false));
        Assert.False(BookingStatusHelper.CanTransition(BookingStatus.Pending, BookingStatus.Completed, RoleNames.Admin, false));
    }

    [Fact]
    public void CanManageListing_admin_always()
    {
        var booking = RoomBooking("owner-1");
        Assert.True(BookingStatusHelper.CanManageListing(booking, "anyone", RoleNames.Admin));
    }

    [Fact]
    public void CanManageListing_owner_only_own_hotel()
    {
        var booking = RoomBooking("owner-1");
        Assert.True(BookingStatusHelper.CanManageListing(booking, "owner-1", RoleNames.HotelOwner));
        Assert.False(BookingStatusHelper.CanManageListing(booking, "other-owner", RoleNames.HotelOwner));
    }

    [Fact]
    public void CanManageListing_agent_only_own_package()
    {
        var booking = new Booking
        {
            TravelPackage = new TravelPackage { AgentId = "agent-1" }
        };
        Assert.True(BookingStatusHelper.CanManageListing(booking, "agent-1", RoleNames.TravelAgent));
        Assert.False(BookingStatusHelper.CanManageListing(booking, "other-agent", RoleNames.TravelAgent));
        Assert.False(BookingStatusHelper.CanManageListing(booking, "agent-1", RoleNames.HotelOwner));
    }

    private static Booking RoomBooking(string ownerId) => new()
    {
        Room = new Room { Hotel = new Hotel { OwnerId = ownerId } }
    };
}
