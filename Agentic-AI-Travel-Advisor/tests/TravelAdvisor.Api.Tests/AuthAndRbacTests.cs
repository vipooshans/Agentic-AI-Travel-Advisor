using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TravelAdvisor.Core.DTOs.Auth;
using Xunit;

namespace TravelAdvisor.Api.Tests;

[Collection("api")]
public class AuthAndRbacTests
{
    private readonly ApiFixture _fx;
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    public AuthAndRbacTests(ApiFixture fx) => _fx = fx;

    [SkippableFact]
    public async Task Register_creates_user_role()
    {
        Skip.If(!_fx.Available, _fx.SkipReason);
        var email = $"user.{Guid.NewGuid():N}@test.com";
        var response = await _fx.Client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "User@123",
            firstName = "Test",
            lastName = "User"
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(Json);
        Assert.Equal("USER", auth!.User.Role);
        Assert.False(string.IsNullOrWhiteSpace(auth.Token));
    }

    [SkippableFact]
    public async Task Login_demo_accounts_succeed_and_bad_password_fails()
    {
        Skip.If(!_fx.Available, _fx.SkipReason);
        var admin = await Login("admin@traveladvisor.com", "Admin@123");
        Assert.Equal("ADMIN", admin.User.Role);

        var owner = await Login("owner@traveladvisor.com", "Owner@123");
        Assert.Equal("HOTEL_OWNER", owner.User.Role);

        var agent = await Login("agent@traveladvisor.com", "Agent@123");
        Assert.Equal("TRAVEL_AGENT", agent.User.Role);

        var bad = await _fx.Client.PostAsJsonAsync("/api/auth/login", new { email = "admin@traveladvisor.com", password = "wrong" });
        Assert.Equal(HttpStatusCode.Unauthorized, bad.StatusCode);
    }

    [SkippableFact]
    public async Task Role_access_is_enforced()
    {
        Skip.If(!_fx.Available, _fx.SkipReason);
        var user = await RegisterUser();
        var owner = await Login("owner@traveladvisor.com", "Owner@123");

        var usersList = await WithToken(user.Token).GetAsync("/api/users");
        Assert.Equal(HttpStatusCode.Forbidden, usersList.StatusCode);

        var createHotel = await WithToken(user.Token).PostAsJsonAsync("/api/hotels", new
        {
            name = "Nope",
            address = "1 St",
            city = "Ella",
            country = "Sri Lanka"
        });
        Assert.Equal(HttpStatusCode.Forbidden, createHotel.StatusCode);

        var createPackage = await WithToken(owner.Token).PostAsJsonAsync("/api/packages", new
        {
            destinationId = 1,
            title = "Nope",
            price = 100,
            durationDays = 2
        });
        Assert.Equal(HttpStatusCode.Forbidden, createPackage.StatusCode);

        var ownerChat = await WithToken(owner.Token).PostAsJsonAsync("/api/ai/chat", new { message = "Plan a trip" });
        Assert.Equal(HttpStatusCode.Forbidden, ownerChat.StatusCode);
    }

    private HttpClient WithToken(string token)
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
