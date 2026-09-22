using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;
using Xunit;

namespace TravelAdvisor.Api.Tests;

public class ApiFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _postgres;
    private WebApplicationFactory<Program>? _factory;

    public bool Available { get; private set; }
    public string SkipReason { get; private set; } = "Docker is required for API integration tests.";
    public HttpClient Client => CreateClient();

    public HttpClient CreateClient()
    {
        if (_factory is null)
            throw new InvalidOperationException(SkipReason);

        var client = _factory.CreateClient();
        client.BaseAddress = new Uri("http://localhost");
        return client;
    }

    public async Task InitializeAsync()
    {
        try
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_URLS", "http://127.0.0.1");
            _postgres = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("travel_advisor_test")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();
            await _postgres.StartAsync();
        }
        catch (Exception ex)
        {
            Available = false;
            SkipReason = $"Docker is required for API integration tests. ({ex.Message})";
            return;
        }

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:DefaultConnection", _postgres.GetConnectionString());
            builder.UseSetting("Jwt:Key", "TravelAdvisorSuperSecretKeyForJwtTokenGeneration2026!");
            builder.UseSetting("Jwt:Issuer", "TravelAdvisor.Api");
            builder.UseSetting("Jwt:Audience", "TravelAdvisor.Clients");
            builder.UseSetting("Jwt:ExpireHours", "24");
            builder.UseSetting("Ai:ApiKey", "");
        });
        _factory.ClientOptions.AllowAutoRedirect = false;
        _factory.ClientOptions.BaseAddress = new Uri("http://localhost");
        Available = true;
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null)
            await _factory.DisposeAsync();
        if (_postgres is not null)
            await _postgres.DisposeAsync();
    }
}

[CollectionDefinition("api")]
public class ApiCollection : ICollectionFixture<ApiFixture>;
