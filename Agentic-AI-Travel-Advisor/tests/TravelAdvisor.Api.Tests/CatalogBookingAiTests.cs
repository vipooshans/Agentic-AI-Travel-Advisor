using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TravelAdvisor.Core.DTOs.Auth;
using TravelAdvisor.Core.DTOs.Bookings;
using TravelAdvisor.Core.DTOs.Destinations;
using TravelAdvisor.Core.DTOs.Hotels;
using TravelAdvisor.Core.DTOs.Itineraries;
using TravelAdvisor.Core.DTOs.Packages;
using TravelAdvisor.Core.Enums;
using Xunit;

namespace TravelAdvisor.Api.Tests;

[Collection("api")]
public class CatalogBookingAiTests
{
    private readonly ApiFixture _fx;
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    public CatalogBookingAiTests(ApiFixture fx) => _fx = fx;

    [SkippableFact]
    public async Task Hotels_pending_hidden_until_admin_approves()
    {
        Skip.If(!_fx.Available, _fx.SkipReason);
        var owner = await Login("owner@traveladvisor.com", "Owner@123");
        var admin = await Login("admin@traveladvisor.com", "Admin@123");
        var name = $"Pending Lodge {Guid.NewGuid():N}"[..28];

        var before = await _fx.Client.GetFromJsonAsync<List<HotelDto>>("/api/hotels", Json);
        var created = await Authed(owner.Token).PostAsJsonAsync("/api/hotels", new
        {
            name,
            address = "1 Test Rd",
            city = "Ella",
            country = "Sri Lanka",
            description = "Awaiting approval"
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var hotel = await created.Content.ReadFromJsonAsync<HotelDto>(Json);
        Assert.Equal(ApprovalStatus.Pending, hotel!.ApprovalStatus);

        var publicAfterCreate = await _fx.Client.GetFromJsonAsync<List<HotelDto>>("/api/hotels", Json);
        Assert.Equal(before!.Count, publicAfterCreate!.Count);
        Assert.DoesNotContain(publicAfterCreate, h => h.Id == hotel.Id);

        var approve = await Authed(admin.Token).PatchAsJsonAsync($"/api/hotels/{hotel.Id}/approval", new { status = 1 });
        Assert.Equal(HttpStatusCode.OK, approve.StatusCode);

        var publicAfterApprove = await _fx.Client.GetFromJsonAsync<List<HotelDto>>("/api/hotels", Json);
        Assert.Contains(publicAfterApprove!, h => h.Id == hotel.Id);
    }

    [SkippableFact]
    public async Task Packages_pending_hidden_until_admin_approves()
    {
        Skip.If(!_fx.Available, _fx.SkipReason);
        var agent = await Login("agent@traveladvisor.com", "Agent@123");
        var admin = await Login("admin@traveladvisor.com", "Admin@123");
        var destinations = await _fx.Client.GetFromJsonAsync<List<DestinationDto>>("/api/destinations", Json);
        Assert.NotEmpty(destinations!);

        var before = await _fx.Client.GetFromJsonAsync<List<TravelPackageDto>>("/api/packages", Json);
        var created = await Authed(agent.Token).PostAsJsonAsync("/api/packages", new
        {
            destinationId = destinations![0].Id,
            title = $"Pending Walk {Guid.NewGuid():N}"[..24],
            description = "Need approval",
            price = 12000,
            durationDays = 2
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var package = await created.Content.ReadFromJsonAsync<TravelPackageDto>(Json);
        Assert.Equal(ApprovalStatus.Pending, package!.ApprovalStatus);

        var publicAfterCreate = await _fx.Client.GetFromJsonAsync<List<TravelPackageDto>>("/api/packages", Json);
        Assert.Equal(before!.Count, publicAfterCreate!.Count);

        var approve = await Authed(admin.Token).PatchAsJsonAsync($"/api/packages/{package.Id}/approval", new { status = 1 });
        Assert.Equal(HttpStatusCode.OK, approve.StatusCode);

        var publicAfterApprove = await _fx.Client.GetFromJsonAsync<List<TravelPackageDto>>("/api/packages", Json);
        Assert.Contains(publicAfterApprove!, p => p.Id == package.Id);
    }

    [SkippableFact]
    public async Task Booking_lifecycle_and_illegal_status()
    {
        Skip.If(!_fx.Available, _fx.SkipReason);
        var user = await RegisterUser();
        var owner = await Login("owner@traveladvisor.com", "Owner@123");
        var hotels = await _fx.Client.GetFromJsonAsync<List<HotelDto>>("/api/hotels", Json);
        var rooms = await _fx.Client.GetFromJsonAsync<List<RoomDto>>($"/api/hotels/{hotels![0].Id}/rooms", Json);
        Assert.NotEmpty(rooms!);

        var checkIn = DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(120), DateTimeKind.Utc);
        var created = await Authed(user.Token).PostAsJsonAsync("/api/bookings", new
        {
            roomId = rooms![0].Id,
            checkIn,
            checkOut = checkIn.AddDays(2)
        });
        Assert.True(created.IsSuccessStatusCode, await created.Content.ReadAsStringAsync());
        var booking = await created.Content.ReadFromJsonAsync<BookingDto>(Json);
        Assert.Equal(BookingStatus.Pending, booking!.Status);

        var illegal = await Authed(owner.Token).PatchAsJsonAsync($"/api/bookings/{booking.Id}/status", new { status = 3 });
        Assert.Equal(HttpStatusCode.BadRequest, illegal.StatusCode);

        var confirm = await Authed(owner.Token).PatchAsJsonAsync($"/api/bookings/{booking.Id}/status", new { status = 1 });
        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);

        var pending = await Authed(user.Token).PostAsJsonAsync("/api/bookings", new
        {
            roomId = rooms[0].Id,
            checkIn = checkIn.AddDays(10),
            checkOut = checkIn.AddDays(11)
        });
        pending.EnsureSuccessStatusCode();
        var pendingBooking = await pending.Content.ReadFromJsonAsync<BookingDto>(Json);
        var cancel = await Authed(user.Token).PatchAsJsonAsync($"/api/bookings/{pendingBooking!.Id}/status", new { status = 2 });
        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
        var cancelled = await cancel.Content.ReadFromJsonAsync<BookingDto>(Json);
        Assert.Equal(BookingStatus.Cancelled, cancelled!.Status);
    }

    [SkippableFact]
    public async Task Ai_chat_user_ok_and_itinerary_saves()
    {
        Skip.If(!_fx.Available, _fx.SkipReason);
        var user = await RegisterUser();

        var chat = await Authed(user.Token).PostAsJsonAsync("/api/ai/chat", new
        {
            message = "Plan a 3-day trip to Ella under Rs. 50000"
        });
        Assert.Equal(HttpStatusCode.OK, chat.StatusCode);

        var saved = await Authed(user.Token).PostAsJsonAsync("/api/itineraries", new
        {
            title = "Ella weekend",
            startDate = DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(30), DateTimeKind.Utc),
            endDate = DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(32), DateTimeKind.Utc),
            estimatedCost = 44000,
            summary = "Test itinerary",
            items = new[]
            {
                new { dayNumber = 1, title = "Nine Arch Bridge", description = "Morning visit", sortOrder = 0 }
            }
        });
        Assert.Equal(HttpStatusCode.Created, saved.StatusCode);

        var list = await Authed(user.Token).GetFromJsonAsync<List<ItineraryDto>>("/api/itineraries", Json);
        Assert.Contains(list!, i => i.Title == "Ella weekend");
    }

    private HttpClient Authed(string token)
    {
        var client = _fx.Client;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task<AuthResponse> Login(string email, string password)
    {
        var response = await _fx.Client.PostAsJsonAsync("/api/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>(Json))!;
    }

    private async Task<AuthResponse> RegisterUser()
    {
        var response = await _fx.Client.PostAsJsonAsync("/api/auth/register", new
        {
            email = $"user.{Guid.NewGuid():N}@test.com",
            password = "User@123",
            firstName = "Test",
            lastName = "User"
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>(Json))!;
    }
}
