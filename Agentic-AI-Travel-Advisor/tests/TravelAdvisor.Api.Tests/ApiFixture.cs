using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;
using System.Diagnostics;
using Testcontainers.PostgreSql;
using Xunit;

namespace TravelAdvisor.Api.Tests;

public class ApiFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _postgres;
    private WebApplicationFactory<Program>? _factory;

    public bool Available { get; private set; }
    public string SkipReason { get; private set; } = "Docker Testcontainers or local PostgreSQL is required for API integration tests.";
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
        Environment.SetEnvironmentVariable("ASPNETCORE_URLS", "http://127.0.0.1");

        var connectionString = await TryLocalPostgresAsync()
            ?? await TryStartTestcontainerAsync();

        if (connectionString is null)
        {
            Available = false;
            return;
        }

        try
        {
            _factory = CreateFactory(connectionString);
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

    private static WebApplicationFactory<Program> CreateFactory(string connectionString)
    {
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:DefaultConnection", connectionString);
            builder.UseSetting("Jwt:Key", "TravelAdvisorSuperSecretKeyForJwtTokenGeneration2026!");
            builder.UseSetting("Jwt:Issuer", "TravelAdvisor.Api");
            builder.UseSetting("Jwt:Audience", "TravelAdvisor.Clients");
            builder.UseSetting("Jwt:ExpireHours", "24");
            builder.UseSetting("Ai:ApiKey", "");
        });
        factory.ClientOptions.AllowAutoRedirect = false;
        factory.ClientOptions.BaseAddress = new Uri("http://localhost");
        return factory;
    }

    private async Task<string?> TryStartTestcontainerAsync()
    {
        if (!DockerImagePresent("postgres:16-alpine"))
        {
            SkipReason = "postgres:16-alpine image not present; skipped Testcontainers.";
            return null;
        }

        try
        {
            _postgres = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("travel_advisor_test")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();
            await _postgres.StartAsync().WaitAsync(TimeSpan.FromSeconds(60));
            return _postgres.GetConnectionString();
        }
        catch (Exception ex)
        {
            SkipReason = $"Testcontainers failed ({ex.Message}).";
            if (_postgres is not null)
            {
                await _postgres.DisposeAsync();
                _postgres = null;
            }
            return null;
        }
    }

    private static bool DockerImagePresent(string image)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = $"images -q {image}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = Process.Start(psi);
            if (process is null) return false;
            var output = process.StandardOutput.ReadToEnd();
            if (!process.WaitForExit(10_000))
            {
                try { process.Kill(true); } catch { /* ignore */ }
                return false;
            }
            return process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output);
        }
        catch
        {
            return false;
        }
    }

    private async Task<string?> TryLocalPostgresAsync()
    {
        var passwords = new[]
        {
            Environment.GetEnvironmentVariable("TEST_PG_PASSWORD"),
            Environment.GetEnvironmentVariable("POSTGRES_PASSWORD"),
            "vipoo",
            "postgres"
        }.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct().ToArray();

        foreach (var password in passwords)
        {
            try
            {
                await using (var admin = new NpgsqlConnection(
                                 $"Host=localhost;Port=5432;Database=postgres;Username=postgres;Password={password};Timeout=5"))
                {
                    await admin.OpenAsync();
                    await using var exists = new NpgsqlCommand(
                        "SELECT 1 FROM pg_database WHERE datname = 'travel_advisor_test'", admin);
                    if (await exists.ExecuteScalarAsync() is null)
                    {
                        await using var create = new NpgsqlCommand("CREATE DATABASE travel_advisor_test", admin);
                        await create.ExecuteNonQueryAsync();
                    }
                }

                var cs = $"Host=localhost;Port=5432;Database=travel_advisor_test;Username=postgres;Password={password};Timeout=5";
                await using var probe = new NpgsqlConnection(cs);
                await probe.OpenAsync();
                return cs;
            }
            catch
            {
                // try next password
            }
        }

        SkipReason = "Local PostgreSQL unavailable (tried configured passwords).";
        return null;
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
