using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using TravelAdvisor.Core.DTOs.Auth;
using TravelAdvisor.Web.Models;

namespace TravelAdvisor.Web.Services;

public class ApiAuthService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ApiAuthService(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
    {
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<(bool Success, string? Error, string? Role)> LoginAsync(LoginViewModel model)
    {
        var client = _httpClientFactory.CreateClient("TravelAdvisorApi");
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = model.Email,
            Password = model.Password
        });

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
            return (false, error?.Message ?? "Login failed.", null);
        }

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        if (auth is null)
        {
            return (false, "Invalid response from server.", null);
        }

        if (auth.User.Role == "USER")
        {
            return (false, "Regular users should use the mobile app.", null);
        }

        await SignInAsync(auth);
        return (true, null, auth.User.Role);
    }

    public async Task LogoutAsync()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context is null) return;

        context.Response.Cookies.Delete("access_token");
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    private async Task SignInAsync(AuthResponse auth)
    {
        var context = _httpContextAccessor.HttpContext!;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, auth.User.Id),
            new(ClaimTypes.Email, auth.User.Email),
            new(ClaimTypes.Name, $"{auth.User.FirstName} {auth.User.LastName}"),
            new(ClaimTypes.Role, auth.User.Role)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await context.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = auth.ExpiresAt
            });

        context.Response.Cookies.Append("access_token", auth.Token, new CookieOptions
        {
            HttpOnly = true,
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Expires = auth.ExpiresAt
        });
    }

    private class ApiErrorResponse
    {
        public string? Message { get; set; }
    }
}
