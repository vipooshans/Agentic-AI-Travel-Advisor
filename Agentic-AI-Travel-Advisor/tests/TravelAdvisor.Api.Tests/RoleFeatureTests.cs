using System.Net;
using System.Net.Http.Json;
using TravelAdvisor.Core.DTOs.Bookings;
using TravelAdvisor.Core.DTOs.Hotels;
using TravelAdvisor.Core.DTOs.Payments;
using TravelAdvisor.Core.DTOs.Reports;
using TravelAdvisor.Core.DTOs.Reviews;
using TravelAdvisor.Core.DTOs.Settings;
using TravelAdvisor.Core.DTOs.Transportation;
using TravelAdvisor.Core.DTOs.Users;
using TravelAdvisor.Core.Enums;
using Xunit;
using static TravelAdvisor.Api.Tests.ApiTestHelpers;

namespace TravelAdvisor.Api.Tests;

/// <summary>Phase 4 role features: reviews, transportation, system settings, simulated payments, profiles and statistics.</summary>
[Collection("api")]
public class RoleFeatureTests(ApiFixture fx)
{
    [SkippableFact]
    public async Task Guest_reviews_a_completed_stay_once_and_admin_moderation_hides_it()
    {
        Skip.If(!fx.Available, fx.SkipReason);
        var (owner, room) = await fx.CreateApprovedRoomWithOwnerAsync();
        var guestAuth = await fx.RegisterUserAsync();
        var guest = fx.Authed(guestAuth.Token);
        var stranger = fx.Authed((await fx.RegisterUserAsync()).Token);
        var admin = fx.Authed((await fx.LoginAsync(AdminEmail, AdminPassword)).Token);

        var future = await BookAndConfirmRoomAsync(owner, guest, room.Id, TodayUtc.AddDays(5));
        var early = await guest.PostAsJsonAsync("/api/reviews", new { bookingId = future.Id, rating = 5, comment = "Too early" });
        Assert.Equal(HttpStatusCode.BadRequest, early.StatusCode);

        var stay = await BookAndConfirmRoomAsync(owner, guest, room.Id, TodayUtc);
        Assert.Equal(HttpStatusCode.OK, (await owner.PatchAsJsonAsync($"/api/bookings/{stay.Id}/status", new { status = 3 })).StatusCode);

        Assert.Equal(HttpStatusCode.NotFound,
            (await stranger.PostAsJsonAsync("/api/reviews", new { bookingId = stay.Id, rating = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await guest.PostAsJsonAsync("/api/reviews", new { bookingId = stay.Id, rating = 6 })).StatusCode);

        var created = await guest.PostAsJsonAsync("/api/reviews", new { bookingId = stay.Id, rating = 4, comment = "Lovely view" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var review = (await created.Content.ReadFromJsonAsync<ReviewDto>(JsonOptions))!;
        Assert.Equal(room.HotelId, review.HotelId);
        Assert.Null(review.TravelPackageId);
        Assert.Equal("Test U.", review.AuthorName);

        Assert.Equal(HttpStatusCode.Conflict,
            (await guest.PostAsJsonAsync("/api/reviews", new { bookingId = stay.Id, rating = 3 })).StatusCode);

        var publicJson = await (await fx.CreateClient().GetAsync($"/api/hotels/{room.HotelId}/reviews")).Content.ReadAsStringAsync();
        Assert.Contains("Lovely view", publicJson);
        Assert.DoesNotContain(guestAuth.User.Email, publicJson, StringComparison.OrdinalIgnoreCase);
        var hotel = await fx.CreateClient().GetFromJsonAsync<HotelDetailDto>($"/api/hotels/{room.HotelId}", JsonOptions);
        Assert.Equal(4.0, hotel!.AverageRating);
        Assert.Equal(1, hotel.ReviewCount);

        Assert.Equal(HttpStatusCode.Forbidden,
            (await stranger.PutAsJsonAsync($"/api/reviews/{review.Id}", new { rating = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await guest.PatchAsJsonAsync($"/api/reviews/{review.Id}/status", new { status = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await owner.PatchAsJsonAsync($"/api/reviews/{review.Id}/status", new { status = 1 })).StatusCode);

        var ownerView = await owner.GetFromJsonAsync<List<ReviewDto>>("/api/reviews", JsonOptions);
        Assert.Contains(ownerView!, r => r.Id == review.Id);

        var hidden = await admin.PatchAsJsonAsync($"/api/reviews/{review.Id}/status", new { status = 1 });
        Assert.Equal(HttpStatusCode.OK, hidden.StatusCode);

        var afterHide = await fx.CreateClient().GetFromJsonAsync<List<ReviewDto>>($"/api/hotels/{room.HotelId}/reviews", JsonOptions);
        Assert.DoesNotContain(afterHide!, r => r.Id == review.Id);
        var hotelAfterHide = await fx.CreateClient().GetFromJsonAsync<HotelDetailDto>($"/api/hotels/{room.HotelId}", JsonOptions);
        Assert.Null(hotelAfterHide!.AverageRating);
        Assert.DoesNotContain((await owner.GetFromJsonAsync<List<ReviewDto>>("/api/reviews", JsonOptions))!, r => r.Id == review.Id);
        Assert.Contains((await guest.GetFromJsonAsync<List<ReviewDto>>("/api/reviews", JsonOptions))!, r => r.Id == review.Id);
        Assert.Contains((await admin.GetFromJsonAsync<List<ReviewDto>>("/api/reviews?status=1", JsonOptions))!, r => r.Id == review.Id);
    }

    [SkippableFact]
    public async Task Package_reviews_target_the_package()
    {
        Skip.If(!fx.Available, fx.SkipReason);
        var (agent, package) = await fx.CreateApprovedPackageWithAgentAsync();
        var guest = fx.Authed((await fx.RegisterUserAsync()).Token);

        var booked = await guest.PostAsJsonAsync("/api/bookings", new { travelPackageId = package.Id, checkIn = TodayUtc });
        Assert.Equal(HttpStatusCode.Created, booked.StatusCode);
        var booking = (await booked.Content.ReadFromJsonAsync<BookingDto>(JsonOptions))!;
        Assert.Equal(HttpStatusCode.OK, (await agent.PatchAsJsonAsync($"/api/bookings/{booking.Id}/status", new { status = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await agent.PatchAsJsonAsync($"/api/bookings/{booking.Id}/status", new { status = 3 })).StatusCode);

        var created = await guest.PostAsJsonAsync("/api/reviews", new { bookingId = booking.Id, rating = 5 });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var review = (await created.Content.ReadFromJsonAsync<ReviewDto>(JsonOptions))!;
        Assert.Equal(package.Id, review.TravelPackageId);
        Assert.Null(review.HotelId);

        var list = await fx.CreateClient().GetFromJsonAsync<List<ReviewDto>>($"/api/packages/{package.Id}/reviews", JsonOptions);
        Assert.Single(list!);
        Assert.Contains((await agent.GetFromJsonAsync<List<ReviewDto>>("/api/reviews", JsonOptions))!, r => r.Id == review.Id);
    }

    [SkippableFact]
    public async Task Agents_manage_only_their_own_transportation_and_inactive_options_are_hidden()
    {
        Skip.If(!fx.Available, fx.SkipReason);
        var (agent, package) = await fx.CreateApprovedPackageWithAgentAsync();
        var (otherAgent, otherPackage) = await fx.CreateApprovedPackageWithAgentAsync();
        var user = fx.Authed((await fx.RegisterUserAsync()).Token);
        var from = $"Test Station {Guid.NewGuid():N}"[..24];

        object Body(int? packageId = null, string departure = "07:30", bool active = true) => new
        {
            travelPackageId = packageId,
            mode = 1,
            fromLocation = from,
            toLocation = "Ella",
            departureTime = departure,
            durationMinutes = 420,
            pricePerPerson = 1500,
            capacity = 40,
            description = "Scenic train",
            isActive = active
        };

        Assert.Equal(HttpStatusCode.Forbidden, (await user.PostAsJsonAsync("/api/transportation", Body())).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await agent.PostAsJsonAsync("/api/transportation", Body(departure: "25:00"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await agent.PostAsJsonAsync("/api/transportation", Body(otherPackage.Id))).StatusCode);

        var created = await agent.PostAsJsonAsync("/api/transportation", Body(package.Id));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var option = (await created.Content.ReadFromJsonAsync<TransportationDto>(JsonOptions))!;
        Assert.Equal(package.DestinationId, option.DestinationId);
        Assert.Equal("07:30", option.DepartureTime);
        Assert.Equal(TransportMode.Train, option.Mode);

        var search = await fx.CreateClient().GetFromJsonAsync<List<TransportationDto>>($"/api/transportation?from={Uri.EscapeDataString(from)}&mode=1", JsonOptions);
        Assert.Single(search!);

        Assert.Equal(HttpStatusCode.Forbidden, (await otherAgent.PutAsJsonAsync($"/api/transportation/{option.Id}", Body())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await otherAgent.DeleteAsync($"/api/transportation/{option.Id}")).StatusCode);

        var deactivated = await agent.PutAsJsonAsync($"/api/transportation/{option.Id}", Body(package.Id, active: false));
        Assert.Equal(HttpStatusCode.OK, deactivated.StatusCode);
        Assert.Empty((await fx.CreateClient().GetFromJsonAsync<List<TransportationDto>>($"/api/transportation?from={Uri.EscapeDataString(from)}", JsonOptions))!);
        Assert.Equal(HttpStatusCode.NotFound, (await fx.CreateClient().GetAsync($"/api/transportation/{option.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await agent.GetAsync($"/api/transportation/{option.Id}")).StatusCode);
        Assert.Contains((await agent.GetFromJsonAsync<List<TransportationDto>>("/api/transportation/mine", JsonOptions))!, t => t.Id == option.Id);
        Assert.DoesNotContain((await otherAgent.GetFromJsonAsync<List<TransportationDto>>("/api/transportation/mine", JsonOptions))!, t => t.Id == option.Id);

        Assert.Equal(HttpStatusCode.NoContent, (await agent.DeleteAsync($"/api/transportation/{option.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await agent.GetAsync($"/api/transportation/{option.Id}")).StatusCode);
    }

    [SkippableFact]
    public async Task Admin_settings_are_validated_and_take_effect()
    {
        Skip.If(!fx.Available, fx.SkipReason);
        var admin = fx.Authed((await fx.LoginAsync(AdminEmail, AdminPassword)).Token);
        var user = fx.Authed((await fx.RegisterUserAsync()).Token);
        var (_, room) = await fx.CreateApprovedRoomWithOwnerAsync();

        Assert.Equal(HttpStatusCode.OK, (await fx.CreateClient().GetAsync("/api/settings/public")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await user.GetAsync("/api/settings")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await user.PutAsJsonAsync($"/api/settings/{SystemSettingKeys.AiAssistantEnabled}", new { value = "false" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PutAsJsonAsync("/api/settings/Jwt.Key", new { value = "x" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync($"/api/settings/{SystemSettingKeys.MaxAdvanceBookingDays}", new { value = "abc" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync($"/api/settings/{SystemSettingKeys.DefaultCurrency}", new { value = "RUPEES" })).StatusCode);

        try
        {
            var off = await admin.PutAsJsonAsync($"/api/settings/{SystemSettingKeys.AiAssistantEnabled}", new { value = "FALSE" });
            Assert.Equal(HttpStatusCode.OK, off.StatusCode);
            Assert.Equal("false", (await off.Content.ReadFromJsonAsync<SystemSettingDto>(JsonOptions))!.Value);
            Assert.False((await fx.CreateClient().GetFromJsonAsync<PublicSettingsDto>("/api/settings/public", JsonOptions))!.AiAssistantEnabled);
            var chat = await user.PostAsJsonAsync("/api/ai/chat", new { message = "Plan a trip to Ella" });
            Assert.Equal(HttpStatusCode.ServiceUnavailable, chat.StatusCode);

            Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync($"/api/settings/{SystemSettingKeys.MaxAdvanceBookingDays}", new { value = "30" })).StatusCode);
            var tooFar = await user.PostAsJsonAsync("/api/bookings", new { roomId = room.Id, checkIn = TodayUtc.AddDays(40), checkOut = TodayUtc.AddDays(41) });
            Assert.Equal(HttpStatusCode.BadRequest, tooFar.StatusCode);
            Assert.Contains("30 days", (await tooFar.ReadJsonAsync()).GetProperty("message").GetString());
        }
        finally
        {
            await admin.PutAsJsonAsync($"/api/settings/{SystemSettingKeys.AiAssistantEnabled}", new { value = "true" });
            await admin.PutAsJsonAsync($"/api/settings/{SystemSettingKeys.MaxAdvanceBookingDays}", new { value = "365" });
        }

        Assert.Equal(HttpStatusCode.OK, (await user.PostAsJsonAsync("/api/ai/chat", new { message = "Plan a trip to Ella" })).StatusCode);
        var all = await admin.GetFromJsonAsync<List<SystemSettingDto>>("/api/settings", JsonOptions);
        Assert.Contains(all!, s => s.Key == SystemSettingKeys.MaxAdvanceBookingDays && s.Value == "365" && s.UpdatedBy != null);
    }

    [SkippableFact]
    public async Task Simulated_payments_settle_decline_confirm_and_refund_on_cancellation()
    {
        Skip.If(!fx.Available, fx.SkipReason);
        var (owner, room) = await fx.CreateApprovedRoomWithOwnerAsync(price: 12000);
        var guest = fx.Authed((await fx.RegisterUserAsync()).Token);
        var stranger = fx.Authed((await fx.RegisterUserAsync()).Token);
        var cardBooking = await BookAndConfirmRoomAsync(owner, guest, room.Id, TodayUtc.AddDays(20));
        var cashBooking = await BookAndConfirmRoomAsync(owner, guest, room.Id, TodayUtc.AddDays(25));
        var cardUrl = $"/api/bookings/{cardBooking.Id}/payments";
        var cashUrl = $"/api/bookings/{cashBooking.Id}/payments";

        Assert.Equal(HttpStatusCode.BadRequest, (await guest.PostAsJsonAsync(cardUrl, new { method = 0, cardNumber = "1234" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await guest.PostAsJsonAsync(cashUrl, new { method = 1, cardNumber = "4242424242424242" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await stranger.PostAsJsonAsync(cardUrl, new { method = 0, cardNumber = "4242424242424242" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await stranger.GetAsync(cardUrl)).StatusCode);

        var declined = await guest.PostAsJsonAsync(cardUrl, new { method = 0, cardNumber = "4000 0000 0000 0002" });
        Assert.Equal(HttpStatusCode.Created, declined.StatusCode);
        Assert.Equal(PaymentStatus.Failed, (await declined.Content.ReadFromJsonAsync<PaymentDto>(JsonOptions))!.Status);

        var paid = await guest.PostAsJsonAsync(cardUrl, new { method = 0, cardNumber = "4242 4242 4242 4242" });
        Assert.Equal(HttpStatusCode.Created, paid.StatusCode);
        var payment = (await paid.Content.ReadFromJsonAsync<PaymentDto>(JsonOptions))!;
        Assert.Equal(PaymentStatus.Completed, payment.Status);
        Assert.Equal(12000m, payment.Amount);
        Assert.StartsWith("SIM-", payment.TransactionReference);
        Assert.NotNull(payment.PaidAt);

        Assert.Equal(HttpStatusCode.Conflict, (await guest.PostAsJsonAsync(cardUrl, new { method = 0, cardNumber = "4242424242424242" })).StatusCode);
        var ownerView = await owner.GetFromJsonAsync<List<PaymentDto>>(cardUrl, JsonOptions);
        Assert.Equal(2, ownerView!.Count);
        Assert.DoesNotContain("4242", await (await owner.GetAsync(cardUrl)).Content.ReadAsStringAsync());

        var cash = await guest.PostAsJsonAsync(cashUrl, new { method = 1 });
        Assert.Equal(HttpStatusCode.Created, cash.StatusCode);
        var cashPayment = (await cash.Content.ReadFromJsonAsync<PaymentDto>(JsonOptions))!;
        Assert.Equal(PaymentStatus.Pending, cashPayment.Status);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await guest.PatchAsJsonAsync($"{cashUrl}/{cashPayment.Id}/status", new { status = 1 })).StatusCode);
        var otherOwner = fx.Authed((await fx.CreateStaffAsync(RoleNames.HotelOwner)).Token);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await otherOwner.PatchAsJsonAsync($"{cashUrl}/{cashPayment.Id}/status", new { status = 1 })).StatusCode);
        var confirmed = await owner.PatchAsJsonAsync($"{cashUrl}/{cashPayment.Id}/status", new { status = 1 });
        Assert.Equal(HttpStatusCode.OK, confirmed.StatusCode);
        Assert.Equal(PaymentStatus.Completed, (await confirmed.Content.ReadFromJsonAsync<PaymentDto>(JsonOptions))!.Status);

        Assert.Equal(HttpStatusCode.OK, (await guest.PatchAsJsonAsync($"/api/bookings/{cardBooking.Id}/status", new { status = 2 })).StatusCode);
        var afterCancel = await guest.GetFromJsonAsync<List<PaymentDto>>(cardUrl, JsonOptions);
        Assert.Contains(afterCancel!, p => p.Id == payment.Id && p.Status == PaymentStatus.Refunded);
        Assert.Equal(HttpStatusCode.BadRequest, (await guest.PostAsJsonAsync(cardUrl, new { method = 0, cardNumber = "4242424242424242" })).StatusCode);
    }

    [SkippableFact]
    public async Task Any_signed_in_user_can_read_and_update_their_own_profile()
    {
        Skip.If(!fx.Available, fx.SkipReason);
        var user = fx.Authed((await fx.RegisterUserAsync()).Token);
        var owner = fx.Authed((await fx.CreateStaffAsync(RoleNames.HotelOwner)).Token);

        var empty = await user.GetFromJsonAsync<UserProfileDto>("/api/users/me/profile", JsonOptions);
        Assert.Equal("LKR", empty!.PreferredCurrency);
        Assert.Null(empty.PhoneNumber);

        var saved = await user.PutAsJsonAsync("/api/users/me/profile", new
        {
            phoneNumber = "+94 77 123 4567",
            nationality = "Sri Lankan",
            dateOfBirth = "1995-04-12",
            bio = "Loves hill country",
            preferredCurrency = "usd"
        });
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        var profile = await user.GetFromJsonAsync<UserProfileDto>("/api/users/me/profile", JsonOptions);
        Assert.Equal("+94 77 123 4567", profile!.PhoneNumber);
        Assert.Equal(new DateOnly(1995, 4, 12), profile.DateOfBirth);
        Assert.Equal("USD", profile.PreferredCurrency);

        Assert.Equal(HttpStatusCode.BadRequest, (await user.PutAsJsonAsync("/api/users/me/profile", new { phoneNumber = "call me" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await user.PutAsJsonAsync("/api/users/me/profile", new { dateOfBirth = "2999-01-01" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await user.PutAsJsonAsync("/api/users/me/profile", new { avatarUrl = "javascript:alert(1)" })).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await owner.PutAsJsonAsync("/api/users/me/profile", new { phoneNumber = "0112345678" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await fx.CreateClient().GetAsync("/api/users/me/profile")).StatusCode);
    }

    [SkippableFact]
    public async Task Statistics_are_scoped_to_the_callers_listings()
    {
        Skip.If(!fx.Available, fx.SkipReason);
        var (owner, room) = await fx.CreateApprovedRoomWithOwnerAsync(price: 10000);
        var otherOwner = fx.Authed((await fx.CreateStaffAsync(RoleNames.HotelOwner)).Token);
        var admin = fx.Authed((await fx.LoginAsync(AdminEmail, AdminPassword)).Token);
        var guest = fx.Authed((await fx.RegisterUserAsync()).Token);

        var kept = await BookAndConfirmRoomAsync(owner, guest, room.Id, TodayUtc.AddDays(8));
        var cancelled = await BookAndConfirmRoomAsync(owner, guest, room.Id, TodayUtc.AddDays(12));
        Assert.Equal(HttpStatusCode.OK, (await owner.PatchAsJsonAsync($"/api/bookings/{cancelled.Id}/status", new { status = 2 })).StatusCode);

        var stats = await owner.GetFromJsonAsync<StatisticsDto>("/api/reports/statistics", JsonOptions);
        Assert.Equal(2, stats!.TotalBookings);
        Assert.Equal(10000m, stats.Revenue);
        Assert.Equal(0.5, stats.CancellationRate);
        Assert.Equal(1, stats.BookingsByStatus[BookingStatus.Confirmed]);
        Assert.Equal(1, stats.BookingsByStatus[BookingStatus.Cancelled]);
        Assert.Equal(12, stats.Monthly.Count);
        Assert.Equal(2, stats.Monthly[^1].Bookings);
        var top = Assert.Single(stats.TopListings);
        Assert.Equal(("Hotel", room.HotelId, 1, 10000m), (top.Type, top.Id, top.Bookings, top.Revenue));
        Assert.NotEqual(kept.Id, cancelled.Id);

        var otherStats = await otherOwner.GetFromJsonAsync<StatisticsDto>("/api/reports/statistics", JsonOptions);
        Assert.Equal(0, otherStats!.TotalBookings);
        Assert.Empty(otherStats.TopListings);

        var adminStats = await admin.GetFromJsonAsync<StatisticsDto>("/api/reports/statistics?top=20", JsonOptions);
        Assert.True(adminStats!.TotalBookings >= 2);

        Assert.Equal(HttpStatusCode.Forbidden, (await guest.GetAsync("/api/reports/statistics")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.GetAsync("/api/reports/statistics?top=0")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await owner.GetAsync($"/api/reports/statistics?from={TodayUtc:yyyy-MM-dd}&to={TodayUtc.AddDays(-1):yyyy-MM-dd}")).StatusCode);
    }
}
