using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using TravelAdvisor.Core.Entities;
using TravelAdvisor.Core.Interfaces;
using TravelAdvisor.Infrastructure.Data;
using TravelAdvisor.Infrastructure.Services;

namespace TravelAdvisor.Infrastructure;

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
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<IJwtTokenService, JwtTokenService>();

        services.Configure<AI.AiOptions>(configuration.GetSection(AI.AiOptions.SectionName));
        services.AddHttpClient<ILlmClient, AI.OpenAiCompatClient>();
        services.AddScoped<ICatalogTools, AI.CatalogTools>();
        services.AddScoped<AI.Agents.TravelPlanningAgent>();
        services.AddScoped<AI.Agents.RecommendationAgent>();
        services.AddScoped<AI.Agents.ItineraryAgent>();
        services.AddScoped<IAgenticAiService, AI.AgenticAiService>();

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
                    RoleClaimType = ClaimTypes.Role
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy("RequireAdmin", policy => policy.RequireRole("ADMIN"))
            .AddPolicy("RequireHotelOwner", policy => policy.RequireRole("HOTEL_OWNER"))
            .AddPolicy("RequireTravelAgent", policy => policy.RequireRole("TRAVEL_AGENT"))
            .AddPolicy("RequireUser", policy => policy.RequireRole("USER"));

        return services;
    }
}
