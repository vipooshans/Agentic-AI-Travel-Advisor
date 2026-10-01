using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TravelAdvisor.Web.Services;
using Xunit;

namespace TravelAdvisor.UnitTests.WebPortal;

/// <summary>
/// DEF-009: the MVC portal must not show a 500 page when the API rejects the stored token
/// or reports a missing resource. The API is replaced by a stub; the portal runs in-process.
/// </summary>
public class PortalApiErrorTests : IClassFixture<PortalApiErrorTests.PortalFactory>
{
    private readonly PortalFactory _factory;

    public PortalApiErrorTests(PortalFactory factory) => _factory = factory;

    [Fact]
    public async Task Def009_expired_api_token_signs_out_and_redirects_to_login()
    {
        _factory.Api.Respond(HttpStatusCode.Unauthorized);
        var client = _factory.CreatePortalClient("HOTEL_OWNER");

        var response = await client.GetAsync("/Owner/Hotels");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Account/Login?ReturnUrl=%2FOwner%2FHotels", response.Headers.Location?.OriginalString);
        var cookies = string.Join("\n", response.Headers.GetValues("Set-Cookie"));
        Assert.Contains("access_token=;", cookies);
        Assert.Contains(".AspNetCore.Cookies=;", cookies);
    }

    [Fact]
    public async Task Def009_expired_api_token_on_a_dashboard_also_redirects_to_login()
    {
        _factory.Api.Respond(HttpStatusCode.Unauthorized);
        var client = _factory.CreatePortalClient("ADMIN");

        var response = await client.GetAsync("/Admin/Users?role=HOTEL_OWNER");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Account/Login?ReturnUrl=%2FAdmin%2FUsers%3Frole%3DHOTEL_OWNER", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Def009_api_404_shows_not_found_instead_of_an_error_page()
    {
        _factory.Api.Respond(HttpStatusCode.NotFound);
        var client = _factory.CreatePortalClient("HOTEL_OWNER");

        var response = await client.GetAsync("/Owner/EditHotel/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Def009_api_403_redirects_to_access_denied()
    {
        _factory.Api.Respond(HttpStatusCode.Forbidden);
        var client = _factory.CreatePortalClient("HOTEL_OWNER");

        var response = await client.GetAsync("/Owner/Rooms/5");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Account/AccessDenied", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Def009_expired_api_token_on_a_form_post_redirects_to_login_without_a_return_url()
    {
        _factory.Api.Respond(HttpStatusCode.OK);
        var client = _factory.CreatePortalClient("HOTEL_OWNER");
        var form = await client.GetAsync("/Owner/CreateHotel");
        var html = await form.Content.ReadAsStringAsync();
        var token = System.Text.RegularExpressions.Regex
            .Match(html, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;
        var antiforgeryCookie = form.Headers.GetValues("Set-Cookie").First(c => c.StartsWith(".AspNetCore.Antiforgery")).Split(';')[0];

        _factory.Api.Respond(HttpStatusCode.Unauthorized);
        var post = new HttpRequestMessage(HttpMethod.Post, "/Owner/UpdateBookingStatus")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["id"] = "3", ["status"] = "1", ["__RequestVerificationToken"] = token
            })
        };
        post.Headers.Remove("Cookie");
        post.Headers.Add("Cookie", $"access_token=expired-token; {antiforgeryCookie}");
        client.DefaultRequestHeaders.Remove("Cookie");

        var response = await client.SendAsync(post);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Account/Login", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Client_reports_rejected_writes_as_false_and_expired_tokens_as_unauthorized()
    {
        var api = new StubApi();
        var client = new TravelApiClient(new StubClientFactory(api));
        var request = new TravelAdvisor.Core.DTOs.Hotels.UpdateHotelRequest { Name = "Hotel", Address = "1 Road", City = "Ella", Country = "Sri Lanka" };

        api.Respond(HttpStatusCode.BadRequest);
        Assert.False(await client.UpdateHotelAsync(1, request));
        Assert.Null(await client.CreateHotelAsync(new TravelAdvisor.Core.DTOs.Hotels.CreateHotelRequest { Name = "Hotel" }));

        api.Respond(HttpStatusCode.NotFound);
        Assert.Null(await client.GetHotelAsync(1));
        Assert.Empty(await client.GetBookingsAsync());

        api.Respond(HttpStatusCode.Unauthorized);
        await Assert.ThrowsAsync<ApiUnauthorizedException>(() => client.UpdateHotelAsync(1, request));
        await Assert.ThrowsAsync<ApiUnauthorizedException>(() => client.GetBookingsAsync());

        api.Respond(HttpStatusCode.Forbidden);
        await Assert.ThrowsAsync<ApiForbiddenException>(() => client.SetHotelApprovalAsync(1, TravelAdvisor.Core.Enums.ApprovalStatus.Approved));
    }

    [Fact]
    public async Task Api_server_errors_are_still_reported_as_errors()
    {
        _factory.Api.Respond(HttpStatusCode.InternalServerError);
        var client = _factory.CreatePortalClient("HOTEL_OWNER");

        var response = await client.GetAsync("/Owner/Hotels");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    public class PortalFactory : WebApplicationFactory<TravelApiClient>
    {
        public StubApi Api { get; } = new();

        public HttpClient CreatePortalClient(string role)
        {
            var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, role);
            client.DefaultRequestHeaders.Add("Cookie", "access_token=expired-token");
            return client;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureTestServices(services =>
            {
                services.AddHttpClient("TravelAdvisorApi").ConfigurePrimaryHttpMessageHandler(() => Api);
                services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
                services.Configure<AuthenticationOptions>(o => o.DefaultAuthenticateScheme = TestAuthHandler.SchemeName);
            });
        }
    }

    public class StubApi : HttpMessageHandler
    {
        private HttpStatusCode _status = HttpStatusCode.OK;

        public void Respond(HttpStatusCode status) => _status = status;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = _status switch
            {
                HttpStatusCode.OK => "[]",
                _ => $"{{\"status\":{(int)_status},\"title\":\"{_status}\"}}"
            };
            return Task.FromResult(new HttpResponseMessage(_status)
            {
                Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json"),
                RequestMessage = request
            });
        }
    }

    private class StubClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false) { BaseAddress = new Uri("http://api.test") };
    }

    /// <summary>Stands in for the portal's auth cookie; the role comes from a request header.</summary>
    public class TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string SchemeName = "Test";
        public const string RoleHeader = "X-Test-Role";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(RoleHeader, out var role))
                return Task.FromResult(AuthenticateResult.NoResult());

            var identity = new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "user-1"), new Claim(ClaimTypes.Role, role.ToString())], SchemeName);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
        }
    }
}
