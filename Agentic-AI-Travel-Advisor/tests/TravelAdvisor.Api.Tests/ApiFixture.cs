using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace TravelAdvisor.Api.Tests;

/// <summary>
/// Boots the real API against a throwaway PostgreSQL database.
/// Order: Testcontainers (fresh container per run) -> local PostgreSQL with a uniquely named database
/// (password from TEST_PG_PASSWORD / POSTGRES_PASSWORD) -> tests are skipped.
/// </summary>
public class ApiFixture : IAsyncLifetime
{
    public const string Issuer = "TravelAdvisor.Api";
    public const string Audience = "TravelAdvisor.Clients";

    private PostgreSqlContainer? _postgres;
    private WebApplicationFactory<Program>? _factory;
    private string? _localAdminConnection;
    private string? _localDatabaseName;

    public bool Available { get; private set; }
    public string SkipReason { get; private set; } = "Docker Testcontainers or local PostgreSQL is required for API integration tests.";
    public string JwtKey { get; } = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
    public string? ConnectionString { get; private set; }
    public string DatabaseSource { get; private set; } = "none";

    public HttpClient Client => CreateClient();

    public WebApplicationFactory<Program> Factory =>
        _factory ?? throw new InvalidOperationException(SkipReason);

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
        Environment.SetEnvironmentVariable("ASPNETCORE_URLS", "http://127.0.0.1");

        ConnectionString = await TryStartTestcontainerAsync() ?? await TryLocalPostgresAsync();

        if (ConnectionString is null)
        {
            Available = false;
            return;
        }

        try
        {
            _factory = CreateFactory(ConnectionString);
            using var warm = _factory.CreateClient();
            var response = await warm.GetAsync("/api/destinations");
            response.EnsureSuccessStatusCode();
            Available = true;
        }
        catch (Exception ex)
        {
            Available = false;
            SkipReason = $"API test host failed to start. ({ex.GetBaseException().Message})";
            if (_factory is not null)
            {
                await _factory.DisposeAsync();
                _factory = null;
            }
        }
    }

    private WebApplicationFactory<Program> CreateFactory(string connectionString)
    {
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:DefaultConnection", connectionString);
            builder.UseSetting("Jwt:Key", JwtKey);
            builder.UseSetting("Jwt:Issuer", Issuer);
            builder.UseSetting("Jwt:Audience", Audience);
            builder.UseSetting("Jwt:ExpireHours", "24");
            builder.UseSetting("Ai:ApiKey", "");
            builder.UseSetting("Seed:DemoUsers", "true");
            builder.UseSetting("RateLimiting:Enabled", "false");
        });
        factory.ClientOptions.AllowAutoRedirect = false;
        factory.ClientOptions.BaseAddress = new Uri("http://localhost");
        return factory;
    }

    private async Task<string?> TryStartTestcontainerAsync()
    {
        if (string.Equals(Environment.GetEnvironmentVariable("TEST_USE_LOCAL_PG"), "true", StringComparison.OrdinalIgnoreCase))
            return null;

        try
        {
            _postgres = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("travel_advisor_test")
                .WithUsername("postgres")
                .WithPassword(Convert.ToHexString(RandomNumberGenerator.GetBytes(12)))
                .Build();
            await _postgres.StartAsync().WaitAsync(TimeSpan.FromSeconds(120));
            DatabaseSource = "testcontainers:postgres:16-alpine";
            return _postgres.GetConnectionString();
        }
        catch (Exception ex)
        {
            SkipReason = $"Testcontainers failed ({ex.GetBaseException().Message}).";
            if (_postgres is not null)
            {
                await _postgres.DisposeAsync();
                _postgres = null;
            }
            return null;
        }
    }

    private async Task<string?> TryLocalPostgresAsync()
    {
        var password = Environment.GetEnvironmentVariable("TEST_PG_PASSWORD")
                       ?? Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");
        if (string.IsNullOrWhiteSpace(password))
        {
            SkipReason += " Local PostgreSQL fallback needs TEST_PG_PASSWORD.";
            return null;
        }

        var host = Environment.GetEnvironmentVariable("TEST_PG_HOST") ?? "localhost";
        var port = Environment.GetEnvironmentVariable("TEST_PG_PORT") ?? "5432";
        _localDatabaseName = $"travel_advisor_test_{Guid.NewGuid():N}"[..40];
        _localAdminConnection = $"Host={host};Port={port};Database=postgres;Username=postgres;Password={password};Timeout=5";

        try
        {
            await using var admin = new NpgsqlConnection(_localAdminConnection);
            await admin.OpenAsync();
            await using var create = new NpgsqlCommand($"CREATE DATABASE \"{_localDatabaseName}\"", admin);
            await create.ExecuteNonQueryAsync();
            DatabaseSource = $"local:{host}:{port}/{_localDatabaseName}";
            return $"Host={host};Port={port};Database={_localDatabaseName};Username=postgres;Password={password};Timeout=5";
        }
        catch (Exception ex)
        {
            SkipReason = $"Local PostgreSQL unavailable ({ex.GetBaseException().Message}).";
            _localAdminConnection = null;
            return null;
        }
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null)
            await _factory.DisposeAsync();
        if (_postgres is not null)
            await _postgres.DisposeAsync();

        if (_localAdminConnection is not null && _localDatabaseName is not null)
        {
            NpgsqlConnection.ClearAllPools();
            await using var admin = new NpgsqlConnection(_localAdminConnection);
            await admin.OpenAsync();
            await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_localDatabaseName}\" WITH (FORCE)", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }
}

[CollectionDefinition("api")]
public class ApiCollection : ICollectionFixture<ApiFixture>;
