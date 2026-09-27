using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TravelAdvisor.Core.Entities;
using TravelAdvisor.Core.Enums;

namespace TravelAdvisor.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await context.Database.MigrateAsync();

        if (!await context.AppRoles.AnyAsync())
        {
            context.AppRoles.AddRange(
                new Role { Name = RoleNames.User },
                new Role { Name = RoleNames.HotelOwner },
                new Role { Name = RoleNames.TravelAgent },
                new Role { Name = RoleNames.Admin });
            await context.SaveChangesAsync();
        }

        if (await userManager.FindByEmailAsync("admin@traveladvisor.com") is null)
        {
            var adminRole = await context.AppRoles.FirstAsync(r => r.Name == RoleNames.Admin);
            var admin = new ApplicationUser
            {
                UserName = "admin@traveladvisor.com",
                Email = "admin@traveladvisor.com",
                FirstName = "System",
                LastName = "Admin",
                RoleId = adminRole.Id,
                EmailConfirmed = true,
                IsActive = true
            };

            await userManager.CreateAsync(admin, "Admin@123");
        }

        if (await userManager.FindByEmailAsync("owner@traveladvisor.com") is null)
        {
            var ownerRole = await context.AppRoles.FirstAsync(r => r.Name == RoleNames.HotelOwner);
            var owner = new ApplicationUser
            {
                UserName = "owner@traveladvisor.com",
                Email = "owner@traveladvisor.com",
                FirstName = "Hotel",
                LastName = "Owner",
                RoleId = ownerRole.Id,
                EmailConfirmed = true,
                IsActive = true
            };

            await userManager.CreateAsync(owner, "Owner@123");
        }

        if (await userManager.FindByEmailAsync("agent@traveladvisor.com") is null)
        {
            var agentRole = await context.AppRoles.FirstAsync(r => r.Name == RoleNames.TravelAgent);
            var agent = new ApplicationUser
            {
                UserName = "agent@traveladvisor.com",
                Email = "agent@traveladvisor.com",
                FirstName = "Travel",
                LastName = "Agent",
                RoleId = agentRole.Id,
                EmailConfirmed = true,
                IsActive = true
            };

            await userManager.CreateAsync(agent, "Agent@123");
        }
    }
}
