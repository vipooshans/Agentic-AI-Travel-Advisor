using System.Security.Claims;
using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using TravelAdvisor.Core.Entities;
using TravelAdvisor.Core.Enums;
using TravelAdvisor.Core.Interfaces;
using TravelAdvisor.Core.Interfaces.Repositories;
using TravelAdvisor.Core.Interfaces.Services;
using TravelAdvisor.Core.Validation;
using TravelAdvisor.Infrastructure.Data;
using TravelAdvisor.Infrastructure.Repositories;
using TravelAdvisor.Infrastructure.Services;

namespace TravelAdvisor.Infrastructure;

public static class AuthPolicies
{
    public const string RequireAdmin = "RequireAdmin";
    public const string RequireHotelOwner = "RequireHotelOwner";
    public const string RequireTravelAgent = "RequireTravelAgent";
    public const string RequireUser = "RequireUser";
    public const string HotelOwnerOrAdmin = "HotelOwnerOrAdmin";
    public const string TravelAgentOrAdmin = "TravelAgentOrAdmin";
    public const string ProviderOrAdmin = "ProviderOrAdmin";
}

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is not configured. Set it with user-secrets or the ConnectionStrings__DefaultConnection environment variable.");

        services.AddDbContext<AppDbContext>(options => options
            .UseNpgsql(connectionString)
            .AddInterceptors(new AuditInterceptor()));

        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 6;
                options.User.RequireUniqueEmail = true;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddMemoryCache();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IUserStatusCache, UserStatusCache>();

        AddRepositories(services);
        AddApplicationServices(services);
        services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

        services.Configure<AI.AiOptions>(configuration.GetSection(AI.AiOptions.SectionName));
        services.AddHttpClient<ILlmClient, AI.OpenAiCompatClient>();
        services.AddScoped<ICatalogTools, AI.CatalogTools>();
        services.AddSingleton<AI.Safety.OutputSanitizer>();
        services.AddScoped<AI.Tools.IAgentTool, AI.Tools.SearchDestinationsTool>();
        services.AddScoped<AI.Tools.IAgentTool, AI.Tools.SearchHotelsTool>();
        services.AddScoped<AI.Tools.IAgentTool, AI.Tools.SearchTravelPackagesTool>();
        services.AddScoped<AI.Tools.IAgentTool, AI.Tools.SearchActivitiesTool>();
        services.AddScoped<AI.Tools.IAgentTool, AI.Tools.SearchTransportationTool>();
        services.AddScoped<AI.Tools.IAgentTool, AI.Tools.CheckAvailabilityTool>();
        services.AddScoped<AI.Tools.IAgentTool, AI.Tools.GetTravelPreferencesTool>();
        services.AddScoped<AI.Tools.IAgentTool, AI.Tools.GenerateItineraryTool>();
        services.AddScoped<AI.Tools.IAgentTool, AI.Tools.CreateBookingTool>();
        services.AddScoped<AI.Tools.ToolRegistry>();
        services.AddScoped<AI.Agents.TravelPlanningAgent>();
        services.AddScoped<AI.Agents.RecommendationAgent>();
        services.AddScoped<AI.Agents.ItineraryAgent>();
        services.AddScoped<AI.Agents.BookingAgent>();
        services.AddScoped<AI.AgentOrchestrator>();
        services.AddScoped<IAgenticAiService, AI.AgenticAiService>();

        AddJwtAuthentication(services, configuration);

        services.AddAuthorizationBuilder()
            .AddPolicy(AuthPolicies.RequireAdmin, policy => policy.RequireRole(RoleNames.Admin))
            .AddPolicy(AuthPolicies.RequireHotelOwner, policy => policy.RequireRole(RoleNames.HotelOwner))
            .AddPolicy(AuthPolicies.RequireTravelAgent, policy => policy.RequireRole(RoleNames.TravelAgent))
            .AddPolicy(AuthPolicies.RequireUser, policy => policy.RequireRole(RoleNames.User))
            .AddPolicy(AuthPolicies.HotelOwnerOrAdmin, policy => policy.RequireRole(RoleNames.HotelOwner, RoleNames.Admin))
            .AddPolicy(AuthPolicies.TravelAgentOrAdmin, policy => policy.RequireRole(RoleNames.TravelAgent, RoleNames.Admin))
            .AddPolicy(AuthPolicies.ProviderOrAdmin, policy => policy.RequireRole(RoleNames.HotelOwner, RoleNames.TravelAgent, RoleNames.Admin));

        return services;
    }

    private static void AddRepositories(IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IHotelRepository, HotelRepository>();
        services.AddScoped<IRoomRepository, RoomRepository>();
        services.AddScoped<IPackageRepository, PackageRepository>();
        services.AddScoped<IDestinationRepository, DestinationRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITravelPreferencesRepository, TravelPreferencesRepository>();
        services.AddScoped<IItineraryRepository, ItineraryRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<IRoomAvailabilityRepository, RoomAvailabilityRepository>();
        services.AddScoped<ISystemSettingRepository, SystemSettingRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<ITransportationRepository, TransportationRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
    }

    private static void AddApplicationServices(IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IHotelService, HotelService>();
        services.AddScoped<IRoomService, RoomService>();
        services.AddScoped<IPackageService, PackageService>();
        services.AddScoped<IDestinationService, DestinationService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IItineraryService, ItineraryService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<ITransportationService, TransportationService>();
        services.AddScoped<ISystemSettingsService, SystemSettingsService>();
        services.AddScoped<IPaymentService, PaymentService>();
    }

    private static void AddJwtAuthentication(IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("Jwt");
        var rawKey = jwtSettings["Key"];
        if (string.IsNullOrWhiteSpace(rawKey) || rawKey.Length < 32)
            throw new InvalidOperationException(
                "Jwt:Key must be configured with at least 32 characters. Set it with user-secrets or the Jwt__Key environment variable.");
        var key = Encoding.UTF8.GetBytes(rawKey);

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ClockSkew = TimeSpan.FromMinutes(1),
                    RoleClaimType = ClaimTypes.Role
                };
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                        var role = context.Principal?.FindFirstValue(ClaimTypes.Role);
                        if (userId is null || role is null)
                        {
                            context.Fail("Token is missing required claims.");
                            return;
                        }

                        var cache = context.HttpContext.RequestServices.GetRequiredService<IUserStatusCache>();
                        var status = await cache.GetAsync(userId, context.HttpContext.RequestAborted);
                        if (status is null || !status.IsActive || status.Role != role)
                            context.Fail("Account is inactive or its role has changed.");
                    },
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        await WriteAuthProblemAsync(context.Response, StatusCodes.Status401Unauthorized,
                            "Unauthorized", "Authentication is required. Sign in and try again.");
                    },
                    OnForbidden = context => WriteAuthProblemAsync(context.Response, StatusCodes.Status403Forbidden,
                        "Forbidden", "You do not have permission to perform this action.")
                };
            });
    }

    private static Task WriteAuthProblemAsync(HttpResponse response, int status, string title, string message)
    {
        if (response.HasStarted)
            return Task.CompletedTask;
        response.StatusCode = status;
        response.ContentType = "application/problem+json";
        return response.WriteAsJsonAsync(new { type = "about:blank", title, status, message }, (System.Text.Json.JsonSerializerOptions?)null, "application/problem+json");
    }
}
