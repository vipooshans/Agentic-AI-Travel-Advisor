using System.Security.Claims;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using TravelAdvisor.Core.Common;
using TravelAdvisor.Core.DTOs.Bookings;
using TravelAdvisor.Core.DTOs.Hotels;
using TravelAdvisor.Core.DTOs.Packages;
using TravelAdvisor.Core.Enums;
using TravelAdvisor.Core.Interfaces;
using TravelAdvisor.Core.Interfaces.Services;
using TravelAdvisor.Infrastructure.AI;
using Xunit;

namespace TravelAdvisor.UnitTests.AI;

public class BookingAgentServiceTests
{
    private readonly Mock<IBookingService> _bookingMock = new();
    private readonly Mock<IHotelService> _hotelMock = new();
    private readonly Mock<IPackageService> _packageMock = new();
    private readonly Mock<ISystemSettingsService> _settingsMock = new();
    private readonly Mock<ILlmClient> _llmClientMock = new();
    private readonly TimeProvider _clock = TimeProvider.System;
    private readonly IOptions<AiOptions> _options = Options.Create(new AiOptions { BookingProposalMinutes = 15 });
    private readonly UserContext _caller = new("traveler-1", "traveler@test.com", "Traveler", RoleNames.User);

    private BookingAgentService CreateService()
    {
        return new BookingAgentService(
            null,
            _bookingMock.Object,
            _hotelMock.Object,
            _packageMock.Object,
            _settingsMock.Object,
            _llmClientMock.Object,
            _clock,
            _options,
            NullLogger<BookingAgentService>.Instance);
    }

    [Fact]
    public async Task Cancellation_prompt_extracts_id_and_returns_preview()
    {
        var booking = new BookingDto
        {
            Id = 15,
            UserId = "traveler-1",
            RoomName = "Deluxe Ocean Suite",
            HotelName = "Galle Lighthouse",
            CheckIn = DateTime.UtcNow.AddDays(5),
            CheckOut = DateTime.UtcNow.AddDays(7),
            TotalPrice = 60000m,
            Status = BookingStatus.Confirmed
        };

        _bookingMock.Setup(b => b.GetAsync(It.IsAny<UserContext>(), 15, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);
        _settingsMock.Setup(s => s.GetIntAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(48);

        var service = CreateService();
        var response = await service.ProcessPromptAsync(_caller, new BookingAgentPromptRequest
        {
            Prompt = "Please cancel my booking #15"
        });

        Assert.Equal("cancellation", response.ActionType);
        Assert.NotNull(response.Cancellation);
        Assert.Equal(15, response.Cancellation.BookingId);
        Assert.True(response.Cancellation.CanCancel);
        Assert.Contains("Galle Lighthouse", response.Cancellation.ListingName);
    }

    [Fact]
    public async Task Insights_prompt_returns_active_booking_alerts()
    {
        var bookings = new List<BookingDto>
        {
            new()
            {
                Id = 101,
                UserId = "traveler-1",
                RoomName = "Mountain Villa",
                HotelName = "Ella Grand",
                CheckIn = DateTime.UtcNow.AddDays(2),
                CheckOut = DateTime.UtcNow.AddDays(4),
                TotalPrice = 45000m,
                Status = BookingStatus.Confirmed
            }
        };

        _bookingMock.Setup(b => b.ListAsync(It.IsAny<UserContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(bookings);
        _settingsMock.Setup(s => s.GetIntAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(48);

        var service = CreateService();
        var response = await service.ProcessPromptAsync(_caller, new BookingAgentPromptRequest
        {
            Prompt = "Show my reservations status and upcoming deadlines"
        });

        Assert.Equal("insights", response.ActionType);
        Assert.NotNull(response.Insights);
        Assert.Single(response.Insights);
        Assert.Equal(101, response.Insights[0].BookingId);
    }

    [Fact]
    public async Task Natural_language_hotel_prompt_generates_valid_proposal()
    {
        var hotels = new List<HotelDto>
        {
            new() { Id = 1, Name = "Ella Heritage Resort", City = "Ella", MinPricePerNight = 25000m }
        };

        var hotelDetail = new HotelDetailDto
        {
            Id = 1,
            Name = "Ella Heritage Resort",
            City = "Ella",
            Rooms =
            [
                new() { Id = 10, HotelId = 1, Name = "Deluxe Room", RoomType = "Deluxe", PricePerNight = 25000m, Capacity = 2, IsAvailable = true }
            ]
        };

        var requestedCheckIn = DateTime.UtcNow.Date.AddDays(10);
        var requestedCheckOut = requestedCheckIn.AddDays(4);
        var quote = new AvailabilityQuoteDto
        {
            Available = true,
            RoomId = 10,
            CheckIn = requestedCheckIn,
            CheckOut = requestedCheckOut,
            Nights = 4,
            Guests = 2,
            TotalPrice = 100000m
        };

        _hotelMock.Setup(h => h.SearchAsync(It.IsAny<HotelSearchQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotels);
        _hotelMock.Setup(h => h.GetAsync(null, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotelDetail);
        _bookingMock.Setup(b => b.CheckAvailabilityAsync(It.IsAny<UserContext>(), It.IsAny<AvailabilityQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(quote);

        // Fallback mode (no LLM configured)
        _llmClientMock.Setup(l => l.IsConfigured).Returns(false);

        var service = CreateService();
        var response = await service.ProcessPromptAsync(_caller, new BookingAgentPromptRequest
        {
            Prompt = $"Book a Deluxe Room in Ella from {requestedCheckIn:yyyy-MM-dd} to {requestedCheckOut:yyyy-MM-dd} for 2 guests"
        });

        Assert.Equal("proposal", response.ActionType);
        Assert.NotNull(response.Proposal);
        Assert.Equal(10, response.Proposal.RoomId);
        Assert.Equal(100000m, response.Proposal.TotalPrice);
        Assert.False(string.IsNullOrWhiteSpace(response.Proposal.ProposalToken));
    }

    [Fact]
    public async Task Incomplete_hotel_request_asks_for_booking_details_without_searching()
    {
        _llmClientMock.Setup(l => l.IsConfigured).Returns(false);

        var service = CreateService();
        var response = await service.ProcessPromptAsync(_caller, new BookingAgentPromptRequest
        {
            Prompt = "I want to book a hotel"
        });

        Assert.Equal("info", response.ActionType);
        Assert.Contains("hotel or destination", response.Message);
        Assert.Contains("check-in date", response.Message);
        Assert.Contains("check-out date", response.Message);
        Assert.Contains("number of guests", response.Message);
        _hotelMock.Verify(h => h.SearchAsync(It.IsAny<UserContext>(), It.IsAny<HotelSearchQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Confirming_valid_proposal_delegates_to_booking_service()
    {
        var created = new BookingDto
        {
            Id = 200,
            UserId = "traveler-1",
            RoomId = 10,
            CheckIn = DateTime.UtcNow.Date.AddDays(10),
            CheckOut = DateTime.UtcNow.Date.AddDays(14),
            Guests = 2,
            TotalPrice = 100000m,
            Status = BookingStatus.Pending
        };

        _bookingMock.Setup(b => b.CreateAsync(It.IsAny<UserContext>(), It.IsAny<CreateBookingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);

        var service = CreateService();

        // Generate valid token
        var checkIn = DateTime.UtcNow.Date.AddDays(10);
        var checkOut = checkIn.AddDays(4);
        var expiresAt = DateTime.UtcNow.AddMinutes(15);
        var raw = $"10::{checkIn:O}:{checkOut:O}:2:100000.00:{expiresAt:O}";
        var token = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(raw));

        var result = await service.ConfirmProposalAsync(_caller, new ConfirmBookingAgentRequest
        {
            ProposalToken = token,
            RoomId = 10,
            CheckIn = checkIn,
            CheckOut = checkOut,
            Guests = 2
        });

        Assert.NotNull(result);
        Assert.Equal(200, result.Id);
        Assert.Equal(BookingStatus.Pending, result.Status);
        _bookingMock.Verify(b => b.CreateAsync(
            It.Is<UserContext>(u => u.UserId == "traveler-1"),
            It.Is<CreateBookingRequest>(r => r.RoomId == 10 && r.Guests == 2),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
