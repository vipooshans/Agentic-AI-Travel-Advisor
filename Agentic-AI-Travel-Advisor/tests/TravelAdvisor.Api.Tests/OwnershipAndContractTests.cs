using System.Net;
using System.Net.Http.Json;
using TravelAdvisor.Core.DTOs.Auth;
using TravelAdvisor.Core.DTOs.Bookings;
using TravelAdvisor.Core.DTOs.Packages;
using TravelAdvisor.Core.Enums;
using Xunit;
using static TravelAdvisor.Api.Tests.ApiTestHelpers;

namespace TravelAdvisor.Api.Tests;

/// <summary>
/// Provider-versus-provider isolation (IDOR), role escalation, mass assignment, registration rules and
/// 404 handling for every resource type, through the real HTTP pipeline and PostgreSQL.
/// </summary>
[Collection("api")]
public class OwnershipAndContractTests(ApiFixture fx)
{
    private static DateTime FreeDate() => TodayUtc.AddDays(60 + Random.Shared.Next(0, 200));

    // ---------- Registration ----------

    [SkippableFact]
    public async Task Duplicate_email_is_rejected_regardless_of_case()
    {
        Skip.If(!fx.Available, fx.SkipReason);
        var email = NewEmail("dup");
        await fx.RegisterUserAsync(email);

        foreach (var attempt in new[] { email, email.ToUpperInvariant(), $"  {email}  " })
        {
            var response = await fx.CreateClient().PostAsJsonAsync("/api/auth/register", new
            {
                email = attempt, password = "User@123", firstName = "Dup", lastName = "User"
            });
            Assert.True(response.StatusCode == HttpStatusCode.Conflict, $"'{attempt}' returned {(int)response.StatusCode}");
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            Assert.Equal("Email is already registered.", (await response.ReadJsonAsync()).GetProperty("message").GetString());
        }
    }

    [SkippableFact]
    public async Task Role_supplied_at_registration_is_ignored()
    {
        Skip.If(!fx.Available, fx.SkipReason);
        var response = await fx.CreateClient().PostAsJsonAsync("/api/auth/register", new
        {
            email = NewEmail("escalate"), password = "User@123", firstName = "Eve", lastName = "Escalate",
            role = RoleNames.Admin, roleId = 4, isActive = true
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var auth = (await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions))!;
        Assert.Equal(RoleNames.User, auth.User.Role);
        Assert.Equal(HttpStatusCode.Forbidden, (await fx.Authed(auth.Token).GetAsync("/api/users")).StatusCode);
    }

    [SkippableTheory]
    [InlineData("Ab1!x", false)]       // 5 characters: below the minimum
    [InlineData("Ab1!xy", true)]       // 6 characters: the minimum
    [InlineData("abcdef1!", false)]    // no upper-case letter
    [InlineData("ABCDEF1!", false)]    // no lower-case letter
    [InlineData("Abcdefg!", false)]    // no digit
    [InlineData("Abcdefg1", false)]    // no symbol
    public async Task Password_policy_boundaries(string password, bool accepted)
    {
        Skip.If(!fx.Available, fx.SkipReason);
        var response = await fx.CreateClient().PostAsJsonAsync("/api/auth/register", new
        {
            email = NewEmail("pw"), password, firstName = "Pass", lastName = "Word"
        });

        if (accepted)
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return;
        }

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await response.ReadJsonAsync()).GetProperty("errors");
        Assert.True(errors.TryGetProperty("password", out _) || errors.TryGetProperty("Password", out _), errors.ToString());
    }

    [SkippableFact]
    public async Task Wrong_password_and_unknown_email_both_return_401_with_the_same_message()
    {
        Skip.If(!fx.Available, fx.SkipReason);
        var wrong = await fx.CreateClient().PostAsJsonAsync("/api/auth/login", new { email = AdminEmail, password = "Nope@123" });
        var unknown = await fx.CreateClient().PostAsJsonAsync("/api/auth/login", new { email = NewEmail("ghost"), password = "Nope@123" });

        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknown.StatusCode);
        Assert.Equal((await wrong.ReadJsonAsync()).GetProperty("message").GetString(), (await unknown.ReadJsonAsync()).GetProperty("message").GetString());
    }

    // ---------- 404s ----------

    [SkippableFact]
    public async Task Unknown_ids_return_404_problem_details_for_every_resource()
    {
        Skip.If(!fx.Available, fx.SkipReason);
        var user = fx.Authed((await fx.RegisterUserAsync()).Token);
        var admin = fx.Authed((await fx.LoginAsync(AdminEmail, AdminPassword)).Token);

        var cases = new (HttpClient Client, string Path)[]
        {
            (fx.CreateClient(), "/api/hotels/999999"),
            (fx.CreateClient(), "/api/hotels/999999/rooms"),
            (fx.CreateClient(), "/api/packages/999999"),
            (fx.CreateClient(), "/api/destinations/999999"),
            (fx.CreateClient(), "/api/transportation/999999"),
            (user, "/api/bookings/999999"),
            (user, "/api/itineraries/999999"),
            (user, "/api/ai/conversations/999999"),
            (admin, "/api/bookings/999999")
        };

        foreach (var (client, path) in cases)
        {
            var response = await client.GetAsync(path);
            Assert.True(response.StatusCode == HttpStatusCode.NotFound, $"{path} returned {(int)response.StatusCode}");
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            var message = (await response.ReadJsonAsync()).GetProperty("message").GetString();
            Assert.EndsWith("not found.", message);
        }

        Assert.Equal(HttpStatusCode.NotFound, (await fx.CreateClient().GetAsync("/api/hotels/not-a-number")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.DeleteAsync("/api/destinations/999999")).StatusCode);
    }

    // ---------- Provider isolation ----------

    [SkippableFact]
    public async Task Agent_cannot_modify_another_agents_package_but_admin_can()
    {
        Skip.If(!fx.Available, fx.SkipReason);
        var (agent, package) = await fx.CreateApprovedPackageWithAgentAsync();
        var otherAgent = fx.Authed((await fx.CreateStaffAsync(RoleNames.TravelAgent)).Token);
        var admin = fx.Authed((await fx.LoginAsync(AdminEmail, AdminPassword)).Token);

        var activity = await agent.PostAsJsonAsync($"/api/packages/{package.Id}/activities", new { title = "Owner's walk", dayNumber = 1, price = 500 });
        Assert.Equal(HttpStatusCode.OK, activity.StatusCode);
        var activityId = (await activity.ReadJsonAsync()).GetProperty("id").GetInt32();

        var update = new
        {
            destinationId = package.DestinationId, title = "Hijacked", description = "x", price = 1, durationDays = 3, maxTravelers = 10
        };
        Assert.Equal(HttpStatusCode.Forbidden, (await otherAgent.PutAsJsonAsync($"/api/packages/{package.Id}", update)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await otherAgent.PostAsJsonAsync($"/api/packages/{package.Id}/activities", new { title = "Sneaky", dayNumber = 1, price = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await otherAgent.DeleteAsync($"/api/packages/{package.Id}/activities/{activityId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await otherAgent.DeleteAsync($"/api/packages/{package.Id}")).StatusCode);

        // The agent's own activity edit sent the package back for review, so only the agent (or an admin) can read it now.
        Assert.Equal(HttpStatusCode.NotFound, (await fx.CreateClient().GetAsync($"/api/packages/{package.Id}")).StatusCode);
        var unchanged = await agent.GetFromJsonAsync<TravelPackageDetailDto>($"/api/packages/{package.Id}", JsonOptions);
        Assert.Equal(package.Title, unchanged!.Title);
        Assert.Equal(ApprovalStatus.Pending, unchanged.ApprovalStatus);
        Assert.Contains(unchanged.Activities, a => a.Id == activityId);
        Assert.DoesNotContain((await otherAgent.GetFromJsonAsync<List<TravelPackageDto>>("/api/packages/mine", JsonOptions))!, p => p.Id == package.Id);

        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/packages/{package.Id}/activities/{activityId}")).StatusCode);
    }

    [SkippableFact]
    public async Task Owner_cannot_touch_another_owners_rooms_or_bookings()
    {
        Skip.If(!fx.Available, fx.SkipReason);
        var (owner, room) = await fx.CreateApprovedRoomWithOwnerAsync();
        var otherOwner = fx.Authed((await fx.CreateStaffAsync(RoleNames.HotelOwner)).Token);
        var guest = fx.Authed((await fx.RegisterUserAsync()).Token);

        var checkIn = FreeDate();
        var created = await guest.PostAsJsonAsync("/api/bookings", new { roomId = room.Id, checkIn, checkOut = checkIn.AddDays(2) });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var booking = (await created.Content.ReadFromJsonAsync<BookingDto>(JsonOptions))!;

        var roomPath = $"/api/hotels/{room.HotelId}/rooms/{room.Id}";
        Assert.Equal(HttpStatusCode.Forbidden, (await otherOwner.PutAsJsonAsync(roomPath, new { name = "Cheap", roomType = "Double", pricePerNight = 1, capacity = 2 })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await otherOwner.PatchAsJsonAsync($"{roomPath}/availability", new { isAvailable = false })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await otherOwner.DeleteAsync(roomPath)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await otherOwner.GetAsync($"/api/bookings/{booking.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await otherOwner.PatchAsJsonAsync($"/api/bookings/{booking.Id}/status", new { status = 1 })).StatusCode);
        Assert.DoesNotContain((await otherOwner.GetFromJsonAsync<List<BookingDto>>("/api/bookings", JsonOptions))!, b => b.Id == booking.Id);

        var stillPending = await guest.GetFromJsonAsync<BookingDto>($"/api/bookings/{booking.Id}", JsonOptions);
        Assert.Equal(BookingStatus.Pending, stillPending!.Status);
        Assert.Contains((await owner.GetFromJsonAsync<List<BookingDto>>("/api/bookings", JsonOptions))!, b => b.Id == booking.Id);
        Assert.Equal(HttpStatusCode.OK, (await owner.PatchAsJsonAsync($"/api/bookings/{booking.Id}/status", new { status = 1 })).StatusCode);
    }

    [SkippableFact]
    public async Task Agent_cannot_manage_a_hotel_booking_and_owner_cannot_manage_a_package_booking()
    {
        Skip.If(!fx.Available, fx.SkipReason);
        var (owner, room) = await fx.CreateApprovedRoomWithOwnerAsync();
        var (agent, package) = await fx.CreateApprovedPackageWithAgentAsync();
        var guest = fx.Authed((await fx.RegisterUserAsync()).Token);

        var checkIn = FreeDate();
        var roomBooking = (await (await guest.PostAsJsonAsync("/api/bookings", new { roomId = room.Id, checkIn, checkOut = checkIn.AddDays(1) }))
            .Content.ReadFromJsonAsync<BookingDto>(JsonOptions))!;
        var packageBooking = (await (await guest.PostAsJsonAsync("/api/bookings", new { travelPackageId = package.Id, checkIn, guests = 1 }))
            .Content.ReadFromJsonAsync<BookingDto>(JsonOptions))!;

        Assert.Equal(HttpStatusCode.Forbidden, (await agent.PatchAsJsonAsync($"/api/bookings/{roomBooking.Id}/status", new { status = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.PatchAsJsonAsync($"/api/bookings/{packageBooking.Id}/status", new { status = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await agent.PatchAsJsonAsync($"/api/bookings/{packageBooking.Id}/status", new { status = 1 })).StatusCode);
    }

    [SkippableFact]
    public async Task Itineraries_and_conversations_are_private_even_from_admins()
    {
        Skip.If(!fx.Available, fx.SkipReason);
        var traveler = fx.Authed((await fx.RegisterUserAsync()).Token);

        var chat = await traveler.PostAsJsonAsync("/api/ai/chat", new { message = "Plan a 3-day trip to Ella under Rs. 50000" });
        Assert.Equal(HttpStatusCode.OK, chat.StatusCode);
        var conversationId = (await chat.ReadJsonAsync()).GetProperty("conversationId").GetInt32();

        var saved = await traveler.PostAsJsonAsync("/api/itineraries", new
        {
            title = "Private Ella weekend",
            startDate = TodayUtc.AddDays(30),
            endDate = TodayUtc.AddDays(32),
            items = new[] { new { dayNumber = 1, title = "Nine Arch Bridge", sortOrder = 0 } }
        });
        Assert.Equal(HttpStatusCode.Created, saved.StatusCode);
        var itineraryId = (await saved.ReadJsonAsync()).GetProperty("id").GetInt32();

        var others = new Dictionary<string, HttpClient>
        {
            ["another traveler"] = fx.Authed((await fx.RegisterUserAsync()).Token),
            [RoleNames.HotelOwner] = fx.Authed((await fx.LoginAsync(OwnerEmail, OwnerPassword)).Token),
            [RoleNames.TravelAgent] = fx.Authed((await fx.LoginAsync(AgentEmail, AgentPassword)).Token),
            [RoleNames.Admin] = fx.Authed((await fx.LoginAsync(AdminEmail, AdminPassword)).Token)
        };

        foreach (var (who, client) in others)
        {
            Assert.True((await client.GetAsync($"/api/itineraries/{itineraryId}")).StatusCode == HttpStatusCode.NotFound, who);
            Assert.True((await client.DeleteAsync($"/api/itineraries/{itineraryId}")).StatusCode == HttpStatusCode.NotFound, who);
            Assert.True((await client.GetAsync($"/api/ai/conversations/{conversationId}")).StatusCode == HttpStatusCode.NotFound, who);

            var itineraries = await (await client.GetAsync("/api/itineraries")).ReadJsonAsync();
            Assert.DoesNotContain(itineraries.EnumerateArray(), i => i.GetProperty("id").GetInt32() == itineraryId);
            var conversations = await (await client.GetAsync("/api/ai/conversations")).ReadJsonAsync();
            Assert.DoesNotContain(conversations.EnumerateArray(), c => c.GetProperty("id").GetInt32() == conversationId);
            var recommendations = await (await client.GetAsync($"/api/ai/recommendations?conversationId={conversationId}")).ReadJsonAsync();
            Assert.True(recommendations.GetArrayLength() == 0, who);
        }

        Assert.Equal(HttpStatusCode.OK, (await traveler.GetAsync($"/api/itineraries/{itineraryId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await traveler.GetAsync($"/api/ai/conversations/{conversationId}")).StatusCode);
    }

    // ---------- Mass assignment ----------

    [SkippableFact]
    public async Task Client_supplied_status_price_and_owner_are_ignored_when_booking()
    {
        Skip.If(!fx.Available, fx.SkipReason);
        var (_, room) = await fx.CreateApprovedRoomWithOwnerAsync(price: 12_500m);
        var victim = await fx.RegisterUserAsync();
        var attacker = await fx.RegisterUserAsync();

        var checkIn = FreeDate();
        var response = await fx.Authed(attacker.Token).PostAsJsonAsync("/api/bookings", new
        {
            roomId = room.Id, checkIn, checkOut = checkIn.AddDays(2), guests = 1,
            status = 1, totalPrice = 1, userId = victim.User.Id, id = 1
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var booking = (await response.Content.ReadFromJsonAsync<BookingDto>(JsonOptions))!;
        Assert.Equal(BookingStatus.Pending, booking.Status);
        Assert.Equal(25_000m, booking.TotalPrice);
        Assert.Equal(attacker.User.Id, booking.UserId);
        Assert.DoesNotContain((await fx.Authed(victim.Token).GetFromJsonAsync<List<BookingDto>>("/api/bookings", JsonOptions))!, b => b.Id == booking.Id);
    }

    [SkippableFact]
    public async Task Provider_cannot_self_approve_a_new_listing()
    {
        Skip.If(!fx.Available, fx.SkipReason);
        var owner = fx.Authed((await fx.CreateStaffAsync(RoleNames.HotelOwner)).Token);

        var created = await owner.PostAsJsonAsync("/api/hotels", new
        {
            name = "Self Approved Inn", address = "1 Rd", city = "Ella", country = "Sri Lanka", approvalStatus = 1
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var hotel = await created.ReadJsonAsync();
        Assert.Equal((int)ApprovalStatus.Pending, hotel.GetProperty("approvalStatus").GetInt32());

        var id = hotel.GetProperty("id").GetInt32();
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.PatchAsJsonAsync($"/api/hotels/{id}/approval", new { status = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await fx.CreateClient().GetAsync($"/api/hotels/{id}")).StatusCode);
    }
}
