using System.Net;
using System.Net.Http.Json;
using Npgsql;
using TravelAdvisor.Core.DTOs.Bookings;
using TravelAdvisor.Core.DTOs.Destinations;
using TravelAdvisor.Core.DTOs.Hotels;
using TravelAdvisor.Core.DTOs.Packages;
using TravelAdvisor.Core.Enums;
using Xunit;
using static TravelAdvisor.Api.Tests.ApiTestHelpers;

namespace TravelAdvisor.Api.Tests;

/// <summary>
/// Booking integrity rules (DEF-002, DEF-006) and re-approval on edit (DEF-010).
/// Every test creates its own approved listing so tests never compete for the same dates.
/// </summary>
[Collection("api")]
public class BookingRulesTests(ApiFixture fx)
{
    private static DateTime Today => DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);

    [SkippableFact]
    public async Task Room_booking_rejects_past_too_far_too_long_and_over_capacity()
    {
        Skip.If(!fx.Available, fx.SkipReason);
        var room = await CreateApprovedRoomAsync(capacity: 2, price: 10000);
        var user = fx.Authed((await fx.RegisterUserAsync()).Token);

        async Task<HttpResponseMessage> Book(DateTime checkIn, DateTime checkOut, int guests = 1) =>
            await user.PostAsJsonAsync("/api/bookings", new { roomId = room.Id, checkIn, checkOut, guests });

        var past = await Book(Today.AddDays(-1), Today.AddDays(1));
        Assert.Equal(HttpStatusCode.BadRequest, past.StatusCode);
        Assert.True((await past.ReadJsonAsync()).GetProperty("errors").TryGetProperty("checkIn", out _));

        var tooFar = await Book(Today.AddDays(400), Today.AddDays(402));
        Assert.Equal(HttpStatusCode.BadRequest, tooFar.StatusCode);

        var tooLong = await Book(Today.AddDays(10), Today.AddDays(41));
        Assert.Equal(HttpStatusCode.BadRequest, tooLong.StatusCode);

        var overCapacity = await Book(Today.AddDays(10), Today.AddDays(12), guests: 3);
        Assert.Equal(HttpStatusCode.BadRequest, overCapacity.StatusCode);
        Assert.True((await overCapacity.ReadJsonAsync()).GetProperty("errors").TryGetProperty("guests", out _));

        var missingCheckIn = await user.PostAsJsonAsync("/api/bookings", new { roomId = room.Id, checkOut = Today.AddDays(3) });
        Assert.Equal(HttpStatusCode.BadRequest, missingCheckIn.StatusCode);

        var ok = await Book(Today.AddDays(10), Today.AddDays(12), guests: 2);
        Assert.Equal(HttpStatusCode.Created, ok.StatusCode);
        var booking = await ok.Content.ReadFromJsonAsync<BookingDto>(Json);
        Assert.Equal(2, booking!.Guests);
        Assert.Equal(20000m, booking.TotalPrice);
        Assert.Equal(Today.AddDays(10), booking.CheckIn.ToUniversalTime());
    }

    [SkippableFact]
    public async Task Room_overlap_returns_409_but_back_to_back_stays_are_allowed()
    {
        Skip.If(!fx.Available, fx.SkipReason);
        var room = await CreateApprovedRoomAsync();
        var first = fx.Authed((await fx.RegisterUserAsync()).Token);
        var second = fx.Authed((await fx.RegisterUserAsync()).Token);
        var checkIn = Today.AddDays(30);

        var a = await first.PostAsJsonAsync("/api/bookings", new { roomId = room.Id, checkIn, checkOut = checkIn.AddDays(3) });
        Assert.Equal(HttpStatusCode.Created, a.StatusCode);

        var overlap = await second.PostAsJsonAsync("/api/bookings", new { roomId = room.Id, checkIn = checkIn.AddDays(2), checkOut = checkIn.AddDays(4) });
        Assert.Equal(HttpStatusCode.Conflict, overlap.StatusCode);

        var backToBack = await second.PostAsJsonAsync("/api/bookings", new { roomId = room.Id, checkIn = checkIn.AddDays(3), checkOut = checkIn.AddDays(5) });
        Assert.Equal(HttpStatusCode.Created, backToBack.StatusCode);
    }

    [SkippableFact]
    public async Task Concurrent_requests_for_the_same_room_and_dates_create_exactly_one_booking()
    {
        Skip.If(!fx.Available, fx.SkipReason);
        var room = await CreateApprovedRoomAsync();
        var users = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => fx.RegisterUserAsync()));
        var checkIn = Today.AddDays(45);

        var responses = await Task.WhenAll(users.Select(u =>
            fx.Authed(u.Token).PostAsJsonAsync("/api/bookings", new { roomId = room.Id, checkIn, checkOut = checkIn.AddDays(2) })));

        var codes = responses.Select(r => r.StatusCode).ToList();
        Assert.Equal(1, codes.Count(c => c == HttpStatusCode.Created));
        Assert.Equal(7, codes.Count(c => c == HttpStatusCode.Conflict));
    }

    [SkippableFact]
    public async Task Database_exclusion_constraint_rejects_overlapping_rows_inserted_directly()
    {
        Skip.If(!fx.Available, fx.SkipReason);
        var room = await CreateApprovedRoomAsync();
        var user = await fx.RegisterUserAsync();
        var checkIn = Today.AddDays(60);

        var created = await fx.Authed(user.Token).PostAsJsonAsync("/api/bookings", new { roomId = room.Id, checkIn, checkOut = checkIn.AddDays(2) });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var booking = await created.Content.ReadFromJsonAsync<BookingDto>(Json);

        await using var connection = new NpgsqlConnection(fx.ConnectionString);
        await connection.OpenAsync();
        await using var insert = new NpgsqlCommand(
            "INSERT INTO \"Bookings\" (\"UserId\", \"RoomId\", \"CheckIn\", \"CheckOut\", \"Guests\", \"Status\", \"TotalPrice\", \"CreatedAt\", \"UpdatedAt\") " +
            "VALUES (@user, @room, @in, @out, 1, 0, 1, now(), now())", connection);
        insert.Parameters.AddWithValue("user", booking!.UserId);
        insert.Parameters.AddWithValue("room", room.Id);
        insert.Parameters.AddWithValue("in", checkIn.AddDays(1));
        insert.Parameters.AddWithValue("out", checkIn.AddDays(3));

        var ex = await Assert.ThrowsAsync<PostgresException>(() => insert.ExecuteNonQueryAsync());
        Assert.Equal(PostgresErrorCodes.ExclusionViolation, ex.SqlState);
    }

    [SkippableFact]
    public async Task Blocked_nights_and_price_overrides_from_the_room_calendar_are_enforced()
    {
        Skip.If(!fx.Available, fx.SkipReason);
        var (owner, room) = await CreateApprovedRoomWithOwnerAsync(price: 10000);
        var user = fx.Authed((await fx.RegisterUserAsync()).Token);
        var start = DateOnly.FromDateTime(Today.AddDays(20));

        var saved = await owner.PutAsJsonAsync($"/api/hotels/{room.HotelId}/rooms/{room.Id}/calendar", new
        {
            entries = new object[]
            {
                new { date = start, isBlocked = false, priceOverride = 15000 },
                new { date = start.AddDays(5), isBlocked = true, note = "Maintenance" }
            }
        });
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);

        var quote = await fx.CreateClient().GetFromJsonAsync<AvailabilityQuoteDto>(
            $"/api/bookings/availability?roomId={room.Id}&checkIn={start:yyyy-MM-dd}&checkOut={start.AddDays(2):yyyy-MM-dd}", Json);
        Assert.True(quote!.Available);
        Assert.Equal(2, quote.Nights);
        Assert.Equal(25000m, quote.TotalPrice);

        var blockedQuote = await fx.CreateClient().GetFromJsonAsync<AvailabilityQuoteDto>(
            $"/api/bookings/availability?roomId={room.Id}&checkIn={start.AddDays(4):yyyy-MM-dd}&checkOut={start.AddDays(7):yyyy-MM-dd}", Json);
        Assert.False(blockedQuote!.Available);
        Assert.Contains(start.AddDays(5).ToString("yyyy-MM-dd"), blockedQuote.Reason);

        var blocked = await user.PostAsJsonAsync("/api/bookings", new
        {
            roomId = room.Id,
            checkIn = start.AddDays(4).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            checkOut = start.AddDays(7).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
        });
        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);

        var priced = await user.PostAsJsonAsync("/api/bookings", new
        {
            roomId = room.Id,
            checkIn = start.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            checkOut = start.AddDays(2).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
        });
        Assert.Equal(HttpStatusCode.Created, priced.StatusCode);
        Assert.Equal(25000m, (await priced.Content.ReadFromJsonAsync<BookingDto>(Json))!.TotalPrice);

        var blockBooked = await owner.PutAsJsonAsync($"/api/hotels/{room.HotelId}/rooms/{room.Id}/calendar", new
        {
            entries = new[] { new { date = start, isBlocked = true } }
        });
        Assert.Equal(HttpStatusCode.Conflict, blockBooked.StatusCode);

        var calendar = await owner.GetFromJsonAsync<RoomCalendarDto>(
            $"/api/hotels/{room.HotelId}/rooms/{room.Id}/calendar?from={start:yyyy-MM-dd}&to={start.AddDays(7):yyyy-MM-dd}", Json);
        Assert.Equal([start, start.AddDays(1)], calendar!.BookedNights);
        Assert.Equal(2, calendar.Overrides.Count);
    }

    [SkippableFact]
    public async Task Another_owner_and_guests_cannot_read_or_change_a_room_calendar()
    {
        Skip.If(!fx.Available, fx.SkipReason);
        var (_, room) = await CreateApprovedRoomWithOwnerAsync();
        var otherOwner = fx.Authed((await fx.CreateStaffAsync(RoleNames.HotelOwner)).Token);
        var user = fx.Authed((await fx.RegisterUserAsync()).Token);
        var url = $"/api/hotels/{room.HotelId}/rooms/{room.Id}/calendar";
        var body = new { entries = new[] { new { date = DateOnly.FromDateTime(Today.AddDays(3)), isBlocked = true } } };

        Assert.Equal(HttpStatusCode.Forbidden, (await otherOwner.PutAsJsonAsync(url, body)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await otherOwner.GetAsync($"{url}?from={Today:yyyy-MM-dd}&to={Today.AddDays(5):yyyy-MM-dd}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await user.PutAsJsonAsync(url, body)).StatusCode);
    }

    [SkippableFact]
    public async Task Package_capacity_duplicates_and_group_pricing_are_enforced()
    {
        Skip.If(!fx.Available, fx.SkipReason);
        var package = await CreateApprovedPackageAsync(price: 10000, maxTravelers: 3);
        var a = fx.Authed((await fx.RegisterUserAsync()).Token);
        var b = fx.Authed((await fx.RegisterUserAsync()).Token);
        var c = fx.Authed((await fx.RegisterUserAsync()).Token);
        var checkIn = Today.AddDays(50);

        Task<HttpResponseMessage> Book(HttpClient client, int guests) =>
            client.PostAsJsonAsync("/api/bookings", new { travelPackageId = package.Id, checkIn, guests });

        var first = await Book(a, 1);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var firstBooking = await first.Content.ReadFromJsonAsync<BookingDto>(Json);
        Assert.Equal(checkIn.AddDays(package.DurationDays), firstBooking!.CheckOut.ToUniversalTime());

        Assert.Equal(HttpStatusCode.Conflict, (await Book(a, 1)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Book(b, 4)).StatusCode);

        var group = await Book(b, 2);
        Assert.Equal(HttpStatusCode.Created, group.StatusCode);
        Assert.Equal(20000m, (await group.Content.ReadFromJsonAsync<BookingDto>(Json))!.TotalPrice);

        var full = await Book(c, 1);
        Assert.Equal(HttpStatusCode.Conflict, full.StatusCode);
        Assert.Contains("fully booked", (await full.ReadJsonAsync()).GetProperty("message").GetString());

        var cancel = await a.PatchAsJsonAsync($"/api/bookings/{firstBooking.Id}/status", new { status = 2 });
        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await Book(c, 1)).StatusCode);
    }

    [SkippableFact]
    public async Task Concurrent_package_bookings_never_exceed_max_travelers()
    {
        Skip.If(!fx.Available, fx.SkipReason);
        var package = await CreateApprovedPackageAsync(maxTravelers: 3);
        var users = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => fx.RegisterUserAsync()));
        var checkIn = Today.AddDays(70);

        var responses = await Task.WhenAll(users.Select(u =>
            fx.Authed(u.Token).PostAsJsonAsync("/api/bookings", new { travelPackageId = package.Id, checkIn, guests = 1 })));

        Assert.Equal(3, responses.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.Equal(3, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));
    }

    [SkippableFact]
    public async Task Guest_can_cancel_confirmed_booking_only_before_the_cutoff()
    {
        Skip.If(!fx.Available, fx.SkipReason);
        var (owner, room) = await CreateApprovedRoomWithOwnerAsync();
        var user = fx.Authed((await fx.RegisterUserAsync()).Token);

        var early = await BookAndConfirmAsync(owner, user, room.Id, Today.AddDays(10));
        var lastMinute = await BookAndConfirmAsync(owner, user, room.Id, Today);

        var allowed = await user.PatchAsJsonAsync($"/api/bookings/{early.Id}/status", new { status = 2 });
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        Assert.NotNull((await allowed.Content.ReadFromJsonAsync<BookingDto>(Json))!.CancelledAt);

        var refused = await user.PatchAsJsonAsync($"/api/bookings/{lastMinute.Id}/status", new { status = 2 });
        Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);
        Assert.Contains("hours before check-in", (await refused.ReadJsonAsync()).GetProperty("message").GetString());

        var providerCancel = await owner.PatchAsJsonAsync($"/api/bookings/{lastMinute.Id}/status", new { status = 2 });
        Assert.Equal(HttpStatusCode.OK, providerCancel.StatusCode);
    }

    [SkippableFact]
    public async Task Booking_cannot_be_completed_before_check_in_or_set_to_its_current_status()
    {
        Skip.If(!fx.Available, fx.SkipReason);
        var (owner, room) = await CreateApprovedRoomWithOwnerAsync();
        var user = fx.Authed((await fx.RegisterUserAsync()).Token);

        var future = await BookAndConfirmAsync(owner, user, room.Id, Today.AddDays(5));
        var current = await BookAndConfirmAsync(owner, user, room.Id, Today);

        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PatchAsJsonAsync($"/api/bookings/{future.Id}/status", new { status = 3 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PatchAsJsonAsync($"/api/bookings/{future.Id}/status", new { status = 1 })).StatusCode);

        var completed = await owner.PatchAsJsonAsync($"/api/bookings/{current.Id}/status", new { status = 3 });
        Assert.Equal(HttpStatusCode.OK, completed.StatusCode);
        Assert.Equal(BookingStatus.Completed, (await completed.Content.ReadFromJsonAsync<BookingDto>(Json))!.Status);
    }

    [SkippableFact]
    public async Task Provider_edits_send_approved_listings_back_for_review_but_admin_edits_do_not()
    {
        Skip.If(!fx.Available, fx.SkipReason);
        var admin = fx.Authed((await fx.LoginAsync(AdminEmail, AdminPassword)).Token);
        var (owner, room) = await CreateApprovedRoomWithOwnerAsync();
        var hotelUrl = $"/api/hotels/{room.HotelId}";
        var hotel = await owner.GetFromJsonAsync<HotelDetailDto>(hotelUrl, Json);

        object HotelBody(string description) => new
        {
            name = hotel!.Name, address = hotel.Address, city = hotel.City, country = hotel.Country, description
        };

        var unchanged = await owner.PutAsJsonAsync(hotelUrl, HotelBody(hotel!.Description!));
        Assert.Equal(ApprovalStatus.Approved, (await unchanged.Content.ReadFromJsonAsync<HotelDto>(Json))!.ApprovalStatus);

        var adminEdit = await admin.PutAsJsonAsync(hotelUrl, HotelBody("Admin fixed a typo"));
        Assert.Equal(ApprovalStatus.Approved, (await adminEdit.Content.ReadFromJsonAsync<HotelDto>(Json))!.ApprovalStatus);

        var ownerEdit = await owner.PutAsJsonAsync(hotelUrl, HotelBody("Now with a rooftop pool"));
        Assert.Equal(ApprovalStatus.Pending, (await ownerEdit.Content.ReadFromJsonAsync<HotelDto>(Json))!.ApprovalStatus);
        Assert.Equal(HttpStatusCode.NotFound, (await fx.CreateClient().GetAsync(hotelUrl)).StatusCode);

        var user = fx.Authed((await fx.RegisterUserAsync()).Token);
        var whilePending = await user.PostAsJsonAsync("/api/bookings", new { roomId = room.Id, checkIn = Today.AddDays(5), checkOut = Today.AddDays(6) });
        Assert.Equal(HttpStatusCode.BadRequest, whilePending.StatusCode);

        var (agent, package) = await CreateApprovedPackageWithAgentAsync();
        var activity = await agent.PostAsJsonAsync($"/api/packages/{package.Id}/activities", new
        {
            title = "Sunset hike", dayNumber = 1, price = 500, sortOrder = 0
        });
        Assert.Equal(HttpStatusCode.OK, activity.StatusCode);
        var afterActivity = await agent.GetFromJsonAsync<TravelPackageDetailDto>($"/api/packages/{package.Id}", Json);
        Assert.Equal(ApprovalStatus.Pending, afterActivity!.ApprovalStatus);
    }

    private Task<BookingDto> BookAndConfirmAsync(HttpClient owner, HttpClient user, int roomId, DateTime checkIn) =>
        BookAndConfirmRoomAsync(owner, user, roomId, checkIn);

    private async Task<RoomDto> CreateApprovedRoomAsync(int capacity = 2, decimal price = 10000) =>
        (await fx.CreateApprovedRoomWithOwnerAsync(capacity, price)).Room;

    private Task<(HttpClient Owner, RoomDto Room)> CreateApprovedRoomWithOwnerAsync(int capacity = 2, decimal price = 10000) =>
        fx.CreateApprovedRoomWithOwnerAsync(capacity, price);

    private async Task<TravelPackageDto> CreateApprovedPackageAsync(decimal price = 10000, int maxTravelers = 10) =>
        (await fx.CreateApprovedPackageWithAgentAsync(price, maxTravelers)).Package;

    private Task<(HttpClient Agent, TravelPackageDto Package)> CreateApprovedPackageWithAgentAsync(decimal price = 10000, int maxTravelers = 10) =>
        fx.CreateApprovedPackageWithAgentAsync(price, maxTravelers);
}
