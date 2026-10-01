using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.IdentityModel.Tokens;
using TravelAdvisor.Core.DTOs.Bookings;
using TravelAdvisor.Core.DTOs.Hotels;
using TravelAdvisor.Core.Enums;
using Xunit;
using static TravelAdvisor.Api.Tests.ApiTestHelpers;

namespace TravelAdvisor.Api.Tests;

/// <summary>Regression tests for authentication, authorization and error-handling defects (DEF-003/004/005/011).</summary>
[Collection("api")]
public class SecurityHardeningTests(ApiFixture fx)
{
    [SkippableFact]
    public async Task Deactivated_user_token_is_rejected_immediately()
    {
        Skip.If(!fx.Available, fx.SkipReason);
        var user = await fx.RegisterUserAsync();
        var admin = await fx.LoginAsync(AdminEmail, AdminPassword);

        Assert.Equal(HttpStatusCode.OK, (await fx.Authed(user.Token).GetAsync("/api/auth/me")).StatusCode);

        var deactivate = await fx.Authed(admin.Token).PatchAsJsonAsync($"/api/users/{user.User.Id}/active", new { isActive = false });
        Assert.Equal(HttpStatusCode.OK, deactivate.StatusCode);

        var me = await fx.Authed(user.Token).GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);

        var login = await fx.CreateClient().PostAsJsonAsync("/api/auth/login", new { email = user.User.Email, password = "User@123" });
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [SkippableFact]
    public async Task Account_locks_after_five_failed_logins()
    {
        Skip.If(!fx.Available, fx.SkipReason);
        var user = await fx.RegisterUserAsync();
        var client = fx.CreateClient();

        for (var i = 0; i < 5; i++)
        {
            var failed = await client.PostAsJsonAsync("/api/auth/login", new { email = user.User.Email, password = "Wrong@123" });
            Assert.Equal(HttpStatusCode.Unauthorized, failed.StatusCode);
        }

        var locked = await client.PostAsJsonAsync("/api/auth/login", new { email = user.User.Email, password = "User@123" });
        Assert.Equal(HttpStatusCode.Unauthorized, locked.StatusCode);
        var body = await locked.ReadJsonAsync();
        Assert.Contains("Too many failed sign-in attempts", body.GetProperty("message").GetString());
    }

    [SkippableFact]
    public async Task Login_email_is_case_insensitive()
    {
        Skip.If(!fx.Available, fx.SkipReason);
        var email = NewEmail("mixed");
        await fx.RegisterUserAsync(email);

        var response = await fx.CreateClient().PostAsJsonAsync("/api/auth/login", new { email = email.ToUpperInvariant(), password = "User@123" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [SkippableFact]
    public async Task Provider_cannot_edit_another_providers_hotel_but_admin_can()
    {
        Skip.If(!fx.Available, fx.SkipReason);
        var owner = await fx.LoginAsync(OwnerEmail, OwnerPassword);
        var otherOwner = await fx.CreateStaffAsync(RoleNames.HotelOwner);
        var admin = await fx.LoginAsync(AdminEmail, AdminPassword);

        var created = await fx.Authed(owner.Token).PostAsJsonAsync("/api/hotels", new
        {
            name = "Ownership Test Inn",
            address = "1 Owner Rd",
            city = "Kandy",
            country = "Sri Lanka"
        });
        var hotel = (await created.Content.ReadFromJsonAsync<HotelDto>(JsonOptions))!;
        var update = new { name = "Renamed Inn", address = "1 Owner Rd", city = "Kandy", country = "Sri Lanka" };

        var byOther = await fx.Authed(otherOwner.Token).PutAsJsonAsync($"/api/hotels/{hotel.Id}", update);
        Assert.Equal(HttpStatusCode.Forbidden, byOther.StatusCode);

        var addRoomByOther = await fx.Authed(otherOwner.Token).PostAsJsonAsync($"/api/hotels/{hotel.Id}/rooms", new
        {
            name = "Sneaky", roomType = "Double", pricePerNight = 100, capacity = 2
        });
        Assert.Equal(HttpStatusCode.Forbidden, addRoomByOther.StatusCode);

        var deleteByOther = await fx.Authed(otherOwner.Token).DeleteAsync($"/api/hotels/{hotel.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, deleteByOther.StatusCode);

        var byAdmin = await fx.Authed(admin.Token).PutAsJsonAsync($"/api/hotels/{hotel.Id}", update);
        Assert.Equal(HttpStatusCode.OK, byAdmin.StatusCode);
    }

    [SkippableFact]
    public async Task Normal_user_cannot_call_admin_apis()
    {
        Skip.If(!fx.Available, fx.SkipReason);
        var user = await fx.RegisterUserAsync();
        var client = fx.Authed(user.Token);

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/users")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PatchAsJsonAsync($"/api/users/{user.User.Id}/active", new { isActive = true })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PatchAsJsonAsync("/api/hotels/1/approval", new { status = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PatchAsJsonAsync("/api/packages/1/approval", new { status = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/destinations", new { name = "X", country = "Y" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.DeleteAsync("/api/destinations/1")).StatusCode);
    }

    [SkippableFact]
    public async Task User_cannot_read_or_change_another_users_booking()
    {
        Skip.If(!fx.Available, fx.SkipReason);
        var alice = await fx.RegisterUserAsync();
        var bob = await fx.RegisterUserAsync();
        var hotels = await fx.CreateClient().GetFromJsonAsync<List<HotelDto>>("/api/hotels", JsonOptions);
        var hotel = hotels!.First(h => h.RoomCount > 0 && h.City == "Galle");
        var room = (await fx.CreateClient().GetFromJsonAsync<List<RoomDto>>($"/api/hotels/{hotel.Id}/rooms", JsonOptions))![^1];

        var checkIn = DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(200 + Random.Shared.Next(0, 100)), DateTimeKind.Utc);
        var created = await fx.Authed(alice.Token).PostAsJsonAsync("/api/bookings", new { roomId = room.Id, checkIn, checkOut = checkIn.AddDays(1) });
        Assert.True(created.IsSuccessStatusCode, await created.Content.ReadAsStringAsync());
        var booking = (await created.Content.ReadFromJsonAsync<BookingDto>(JsonOptions))!;

        Assert.Equal(HttpStatusCode.Forbidden, (await fx.Authed(bob.Token).GetAsync($"/api/bookings/{booking.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await fx.Authed(bob.Token).PatchAsJsonAsync($"/api/bookings/{booking.Id}/status", new { status = 2 })).StatusCode);

        var bobsList = await fx.Authed(bob.Token).GetFromJsonAsync<List<BookingDto>>("/api/bookings", JsonOptions);
        Assert.DoesNotContain(bobsList!, b => b.Id == booking.Id);
    }

    [SkippableFact]
    public async Task Invalid_expired_and_unsigned_tokens_are_rejected()
    {
        Skip.If(!fx.Available, fx.SkipReason);
        var user = await fx.RegisterUserAsync();

        var wrongKey = CreateToken(user.User.Id, RoleNames.User, Convert.ToBase64String(new byte[48]), DateTime.UtcNow.AddHours(1));
        var expired = CreateToken(user.User.Id, RoleNames.User, fx.JwtKey, DateTime.UtcNow.AddMinutes(-10));
        var escalated = CreateToken(user.User.Id, RoleNames.Admin, fx.JwtKey, DateTime.UtcNow.AddHours(1));
        var unsigned = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            ApiFixture.Issuer, ApiFixture.Audience,
            [new Claim(ClaimTypes.NameIdentifier, user.User.Id), new Claim(ClaimTypes.Role, RoleNames.Admin)],
            expires: DateTime.UtcNow.AddHours(1)));

        foreach (var token in new[] { "not-a-jwt", wrongKey, expired, unsigned })
        {
            var response = await fx.Authed(token).GetAsync("/api/auth/me");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.False(string.IsNullOrWhiteSpace((await response.ReadJsonAsync()).GetProperty("message").GetString()));
        }

        // A correctly signed token whose role no longer matches the database is refused as well.
        Assert.Equal(HttpStatusCode.Unauthorized, (await fx.Authed(escalated).GetAsync("/api/users")).StatusCode);
    }

    [SkippableFact]
    public async Task Validation_errors_return_problem_details_with_message_and_errors()
    {
        Skip.If(!fx.Available, fx.SkipReason);
        var response = await fx.CreateClient().PostAsJsonAsync("/api/auth/register", new
        {
            email = "not-an-email",
            password = "",
            firstName = "",
            lastName = "User"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.ReadJsonAsync();
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("message").GetString()));
        Assert.True(body.GetProperty("errors").EnumerateObject().Any());
    }

    [SkippableFact]
    public async Task Unknown_resource_returns_problem_details_without_internals()
    {
        Skip.If(!fx.Available, fx.SkipReason);
        var response = await fx.CreateClient().GetAsync("/api/hotels/999999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.ReadJsonAsync();
        Assert.Equal("Hotel not found.", body.GetProperty("message").GetString());
        Assert.DoesNotContain("Exception", body.ToString());
    }

    [SkippableFact]
    public async Task Responses_carry_security_headers()
    {
        Skip.If(!fx.Available, fx.SkipReason);
        foreach (var path in new[] { "/api/destinations", "/api/hotels/999999" })
        {
            var response = await fx.CreateClient().GetAsync(path);
            Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
            Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        }
    }

    [SkippableFact]
    public async Task Swagger_is_disabled_outside_development_by_default()
    {
        Skip.If(!fx.Available, fx.SkipReason);
        var response = await fx.CreateClient().GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [SkippableFact]
    public async Task Login_endpoint_is_rate_limited_when_enabled()
    {
        Skip.If(!fx.Available, fx.SkipReason);
        await using var limited = fx.Factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("RateLimiting:Enabled", "true");
            builder.UseSetting("RateLimiting:AuthPermitsPerMinute", "3");
        });
        var client = limited.CreateClient();

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 5; i++)
        {
            var response = await client.PostAsJsonAsync("/api/auth/login", new { email = NewEmail(), password = "Wrong@123" });
            statuses.Add(response.StatusCode);
        }

        Assert.Equal(3, statuses.Count(s => s == HttpStatusCode.Unauthorized));
        Assert.Equal(2, statuses.Count(s => s == HttpStatusCode.TooManyRequests));
    }

    private static string CreateToken(string userId, string role, string key, DateTime expires)
    {
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            ApiFixture.Issuer,
            ApiFixture.Audience,
            [new Claim(ClaimTypes.NameIdentifier, userId), new Claim(ClaimTypes.Role, role)],
            notBefore: expires.AddHours(-2),
            expires: expires,
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
