using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi.Models;
using TravelAdvisor.Api.Infrastructure;
using TravelAdvisor.Infrastructure;
using TravelAdvisor.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddControllers(options => options.Filters.Add<ValidationFilter>())
    .ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = ValidationResponses.FromModelState);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Agentic AI Travel Advisor API",
        Version = "v1",
        Description = "REST API for travelers, hotel owners, travel agents and administrators."
    });
    var bearer = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the token returned by POST /api/auth/login.",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };
    options.AddSecurityDefinition("Bearer", bearer);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement { [bearer] = [] });
});

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowClients", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

AddRateLimiting(builder.Services, builder.Configuration);

var app = builder.Build();

// Registered via OnStarting so the headers survive the exception handler clearing the response.
app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        var headers = context.Response.Headers;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "no-referrer";
        if (!context.Request.Path.StartsWithSegments("/swagger"))
        {
            headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
            headers["Cache-Control"] = "no-store";
        }
        return Task.CompletedTask;
    });
    await next();
});
app.UseExceptionHandler();

var swaggerEnabled = app.Configuration.GetValue<bool?>("Swagger:Enabled") ?? app.Environment.IsDevelopment();
if (swaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowClients");
if (!HttpOnlyUrls() && !app.Environment.IsEnvironment("Testing"))
    app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();

var seedDemoUsers = app.Configuration.GetValue<bool?>("Seed:DemoUsers") ?? app.Environment.IsDevelopment();
await DbSeeder.SeedAsync(app.Services, seedDemoUsers);

app.Run();

static bool HttpOnlyUrls()
{
    var urls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? string.Empty;
    return urls.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
           && !urls.Contains("https://", StringComparison.OrdinalIgnoreCase);
}

static void AddRateLimiting(IServiceCollection services, IConfiguration configuration)
{
    var enabled = configuration.GetValue("RateLimiting:Enabled", true);
    var authPermits = configuration.GetValue("RateLimiting:AuthPermitsPerMinute", 10);
    var aiPermits = configuration.GetValue("RateLimiting:AiPermitsPerMinute", 20);

    services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.OnRejected = async (context, _) =>
        {
            if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
            await ProblemWriter.WriteAsync(context.HttpContext, StatusCodes.Status429TooManyRequests,
                "Too many requests. Please wait a moment and try again.");
        };

        options.AddPolicy(RateLimitPolicies.Auth, http => enabled
            ? RateLimitPartition.GetFixedWindowLimiter(
                http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = authPermits, Window = TimeSpan.FromMinutes(1) })
            : RateLimitPartition.GetNoLimiter("disabled"));

        options.AddPolicy(RateLimitPolicies.Ai, http => enabled
            ? RateLimitPartition.GetSlidingWindowLimiter(
                http.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = aiPermits,
                    Window = TimeSpan.FromMinutes(1),
                    SegmentsPerWindow = 6
                })
            : RateLimitPartition.GetNoLimiter("disabled"));
    });
}

public partial class Program;
