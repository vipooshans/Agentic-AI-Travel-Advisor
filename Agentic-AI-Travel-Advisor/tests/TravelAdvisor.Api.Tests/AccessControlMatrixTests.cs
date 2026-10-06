using System.Net;
using System.Net.Http.Json;
using System.Text;
using TravelAdvisor.Core.Enums;
using Xunit;
using Xunit.Abstractions;
using static TravelAdvisor.Api.Tests.ApiTestHelpers;

namespace TravelAdvisor.Api.Tests;

/// <summary>
/// Calls every protected endpoint as anonymous, USER, HOTEL_OWNER, TRAVEL_AGENT and ADMIN and checks the
/// authorization outcome: anonymous gets 401, a role outside the policy gets 403, and an allowed role gets
/// past authorization (any status other than 401/403). Write endpoints use unknown ids or empty bodies, so
/// allowed roles hit validation or 404 and nothing in the seeded data changes.
/// </summary>
[Collection("api")]
public class AccessControlMatrixTests(ApiFixture fx, ITestOutputHelper output)
{
    private const string Anonymous = "ANONYMOUS";
    private const string Missing = "999999";

    private static readonly string[] Everyone = RoleNames.All;
    private static readonly string[] Admin = [RoleNames.Admin];
    private static readonly string[] User = [RoleNames.User];
    private static readonly string[] Owner = [RoleNames.HotelOwner];
    private static readonly string[] Agent = [RoleNames.TravelAgent];
    private static readonly string[] OwnerOrAdmin = [RoleNames.HotelOwner, RoleNames.Admin];
    private static readonly string[] AgentOrAdmin = [RoleNames.TravelAgent, RoleNames.Admin];
    private static readonly string[] ProviderOrAdmin = [RoleNames.HotelOwner, RoleNames.TravelAgent, RoleNames.Admin];

    private sealed record Endpoint(string Method, string Path, string[] Allowed, string? Body = "{}");

    private static readonly Endpoint[] Protected =
    [
        // Auth and profile
        new("GET", "/api/auth/me", Everyone, null),
        new("PUT", "/api/auth/me", Everyone),
        new("GET", "/api/users/me/profile", Everyone, null),
        new("GET", "/api/users/me/preferences", User, null),
        new("PUT", "/api/users/me/preferences", User, """{"budgetMin":-1}"""),
        // Admin user management
        new("GET", "/api/users", Admin, null),
        new("POST", "/api/users", Admin),
        new("PATCH", $"/api/users/{Missing}/active", Admin, """{"isActive":true}"""),
        // Hotels and rooms
        new("GET", "/api/hotels/mine", Owner, null),
        new("POST", "/api/hotels", Owner),
        new("PUT", $"/api/hotels/{Missing}", OwnerOrAdmin),
        new("DELETE", $"/api/hotels/{Missing}", OwnerOrAdmin, null),
        new("PATCH", $"/api/hotels/{Missing}/approval", Admin, """{"status":1}"""),
        new("POST", $"/api/hotels/{Missing}/rooms", OwnerOrAdmin),
        new("PUT", $"/api/hotels/{Missing}/rooms/1", OwnerOrAdmin),
        new("PATCH", $"/api/hotels/{Missing}/rooms/1/availability", OwnerOrAdmin, """{"isAvailable":true}"""),
        new("GET", $"/api/hotels/{Missing}/rooms/1/calendar?from=2030-01-01&to=2030-01-10", OwnerOrAdmin, null),
        new("PUT", $"/api/hotels/{Missing}/rooms/1/calendar", OwnerOrAdmin),
        new("DELETE", $"/api/hotels/{Missing}/rooms/1", OwnerOrAdmin, null),
        // Packages and activities
        new("GET", "/api/packages/mine", Agent, null),
        new("POST", "/api/packages", Agent),
        new("PUT", $"/api/packages/{Missing}", AgentOrAdmin),
        new("DELETE", $"/api/packages/{Missing}", AgentOrAdmin, null),
        new("POST", $"/api/packages/{Missing}/activities", AgentOrAdmin),
        new("DELETE", $"/api/packages/{Missing}/activities/1", AgentOrAdmin, null),
        new("PATCH", $"/api/packages/{Missing}/approval", Admin, """{"status":1}"""),
        // Destinations, transportation, settings, reports
        new("POST", "/api/destinations", Admin),
        new("PUT", $"/api/destinations/{Missing}", Admin),
        new("DELETE", $"/api/destinations/{Missing}", Admin, null),
        new("GET", "/api/transportation/mine", AgentOrAdmin, null),
        new("POST", "/api/transportation", AgentOrAdmin),
        new("PUT", $"/api/transportation/{Missing}", AgentOrAdmin),
        new("DELETE", $"/api/transportation/{Missing}", AgentOrAdmin, null),
        new("GET", "/api/settings", Admin, null),
        new("PUT", "/api/settings/Unknown.Key", Admin, """{"value":""}"""),
        new("GET", "/api/reports/summary", Everyone, null),
        new("GET", "/api/reports/statistics", ProviderOrAdmin, null),
        // Bookings and payments
        new("POST", "/api/bookings", User),
        new("GET", "/api/bookings", Everyone, null),
        new("GET", $"/api/bookings/{Missing}", Everyone, null),
        new("PATCH", $"/api/bookings/{Missing}/status", Everyone, """{"status":2}"""),
        new("GET", $"/api/bookings/{Missing}/payments", Everyone, null),
        new("POST", $"/api/bookings/{Missing}/payments", User),
        new("PATCH", $"/api/bookings/{Missing}/payments/1/status", ProviderOrAdmin),
        // AI and itineraries. Only travelers can create them; the read/delete endpoints are documented as
        // "Authenticated" and are always filtered to the caller's own records (see
        // OwnershipAndContractTests.Itineraries_and_conversations_are_private_even_from_admins).
        new("POST", "/api/ai/chat", User),
        new("GET", "/api/ai/conversations", Everyone, null),
        new("GET", $"/api/ai/conversations/{Missing}", Everyone, null),
        new("GET", "/api/ai/recommendations", Everyone, null),
        new("POST", "/api/itineraries", User),
        new("GET", "/api/itineraries", Everyone, null),
        new("GET", $"/api/itineraries/{Missing}", Everyone, null),
        new("DELETE", $"/api/itineraries/{Missing}", Everyone, null),
        // Reviews
        new("GET", "/api/reviews", Everyone, null),
        new("POST", "/api/reviews", User),
        new("PUT", $"/api/reviews/{Missing}", User),
        new("DELETE", $"/api/reviews/{Missing}", Everyone, null),
        new("PATCH", $"/api/reviews/{Missing}/status", Admin, """{"status":1}""")
    ];

    private static readonly string[] Public =
    [
        "/api/health",
        "/api/destinations",
        "/api/hotels",
        "/api/packages",
        "/api/transportation",
        "/api/settings/public",
        "/api/hotels/1/reviews",
        "/api/packages/1/reviews",
        $"/api/bookings/availability?roomId=1&checkIn={DateTime.UtcNow.Date.AddDays(30):yyyy-MM-dd}&checkOut={DateTime.UtcNow.Date.AddDays(31):yyyy-MM-dd}&guests=1"
    ];

    private async Task<Dictionary<string, string?>> TokensAsync() => new()
    {
        [Anonymous] = null,
        [RoleNames.User] = (await fx.RegisterUserAsync()).Token,
        [RoleNames.HotelOwner] = (await fx.LoginAsync(OwnerEmail, OwnerPassword)).Token,
        [RoleNames.TravelAgent] = (await fx.LoginAsync(AgentEmail, AgentPassword)).Token,
        [RoleNames.Admin] = (await fx.LoginAsync(AdminEmail, AdminPassword)).Token
    };

    private async Task<HttpStatusCode> SendAsync(string? token, Endpoint endpoint)
    {
        var client = token is null ? fx.CreateClient() : fx.Authed(token);
        using var request = new HttpRequestMessage(new HttpMethod(endpoint.Method), endpoint.Path);
        if (endpoint.Body is not null)
            request.Content = new StringContent(endpoint.Body, Encoding.UTF8, "application/json");
        using var response = await client.SendAsync(request);
        return response.StatusCode;
    }

    [SkippableFact]
    public async Task Every_protected_endpoint_enforces_its_role_policy()
    {
        Skip.If(!fx.Available, fx.SkipReason);
        var tokens = await TokensAsync();
        var failures = new List<string>();
        var checks = 0;

        foreach (var endpoint in Protected)
        {
            var row = new StringBuilder($"{endpoint.Method,-6} {endpoint.Path,-70}");
            foreach (var (caller, token) in tokens)
            {
                var status = await SendAsync(token, endpoint);
                checks++;
                row.Append($" {caller}={(int)status}");

                var expectation = caller == Anonymous ? "401"
                    : endpoint.Allowed.Contains(caller) ? "not 401/403"
                    : "403";
                var ok = expectation switch
                {
                    "401" => status == HttpStatusCode.Unauthorized,
                    "403" => status == HttpStatusCode.Forbidden,
                    _ => status is not HttpStatusCode.Unauthorized and not HttpStatusCode.Forbidden
                };
                if (!ok)
                    failures.Add($"{endpoint.Method} {endpoint.Path} as {caller}: expected {expectation}, got {(int)status}");
            }
            output.WriteLine(row.ToString());
        }

        output.WriteLine($"{Protected.Length} endpoints x {tokens.Count} callers = {checks} checks, {failures.Count} failures");
        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    [SkippableFact]
    public async Task Public_catalog_endpoints_do_not_require_a_token()
    {
        Skip.If(!fx.Available, fx.SkipReason);
        foreach (var path in Public)
        {
            var response = await fx.CreateClient().GetAsync(path);
            output.WriteLine($"GET {path} anonymous={(int)response.StatusCode}");
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"{path} returned {(int)response.StatusCode}");
        }
    }

    [SkippableFact]
    public async Task Malformed_authorization_headers_are_rejected()
    {
        Skip.If(!fx.Available, fx.SkipReason);
        var user = await fx.RegisterUserAsync();

        foreach (var header in new[] { "Bearer", "Bearer ", $"Basic {user.Token}", user.Token, $"Bearer {user.Token}x", $"Bearer {user.Token[..^5]}" })
        {
            var client = fx.CreateClient();
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", header);
            var response = await client.GetAsync("/api/auth/me");
            Assert.True(response.StatusCode == HttpStatusCode.Unauthorized, $"'{Shorten(header)}' returned {(int)response.StatusCode}");
        }

        Assert.Equal(HttpStatusCode.OK, (await fx.Authed(user.Token).GetAsync("/api/auth/me")).StatusCode);
    }

    private static string Shorten(string value) => value.Length > 30 ? value[..30] + "..." : value;
}
