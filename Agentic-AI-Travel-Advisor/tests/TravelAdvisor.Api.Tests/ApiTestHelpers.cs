using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TravelAdvisor.Core.DTOs.Auth;

namespace TravelAdvisor.Api.Tests;

internal static class ApiTestHelpers
{
    public static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    public const string AdminEmail = "admin@traveladvisor.com";
    public const string AdminPassword = "Admin@123";
    public const string OwnerEmail = "owner@traveladvisor.com";
    public const string OwnerPassword = "Owner@123";
    public const string AgentEmail = "agent@traveladvisor.com";
    public const string AgentPassword = "Agent@123";

    public static HttpClient Authed(this ApiFixture fx, string token)
    {
        var client = fx.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public static async Task<AuthResponse> LoginAsync(this ApiFixture fx, string email, string password)
    {
        var response = await fx.CreateClient().PostAsJsonAsync("/api/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>(Json))!;
    }

    public static async Task<AuthResponse> RegisterUserAsync(this ApiFixture fx, string? email = null, string password = "User@123")
    {
        var response = await fx.CreateClient().PostAsJsonAsync("/api/auth/register", new
        {
            email = email ?? NewEmail(),
            password,
            firstName = "Test",
            lastName = "User"
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>(Json))!;
    }

    /// <summary>Creates a staff account through the admin API and signs in as it.</summary>
    public static async Task<AuthResponse> CreateStaffAsync(this ApiFixture fx, string role)
    {
        var admin = await fx.LoginAsync(AdminEmail, AdminPassword);
        var email = NewEmail(role.ToLowerInvariant());
        var created = await fx.Authed(admin.Token).PostAsJsonAsync("/api/users", new
        {
            email,
            password = "Staff@123",
            firstName = "Staff",
            lastName = role,
            role
        });
        created.EnsureSuccessStatusCode();
        return await fx.LoginAsync(email, "Staff@123");
    }

    public static string NewEmail(string prefix = "user") => $"{prefix}.{Guid.NewGuid():N}@test.com";

    public static async Task<JsonElement> ReadJsonAsync(this HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(string.IsNullOrWhiteSpace(text) ? "{}" : text).RootElement;
    }
}
