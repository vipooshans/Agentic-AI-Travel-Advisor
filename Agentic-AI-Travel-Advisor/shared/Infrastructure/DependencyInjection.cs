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
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

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
        var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

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
