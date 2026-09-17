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
            var ownerUser = new ApplicationUser
            {
                UserName = "owner@traveladvisor.com",
                Email = "owner@traveladvisor.com",
                FirstName = "Hotel",
                LastName = "Owner",
                RoleId = ownerRole.Id,
                EmailConfirmed = true,
                IsActive = true
            };
            await userManager.CreateAsync(ownerUser, "Owner@123");
        }

        if (await userManager.FindByEmailAsync("agent@traveladvisor.com") is null)
        {
            var agentRole = await context.AppRoles.FirstAsync(r => r.Name == RoleNames.TravelAgent);
            var agentUser = new ApplicationUser
            {
                UserName = "agent@traveladvisor.com",
                Email = "agent@traveladvisor.com",
                FirstName = "Travel",
                LastName = "Agent",
                RoleId = agentRole.Id,
                EmailConfirmed = true,
                IsActive = true
            };
            await userManager.CreateAsync(agentUser, "Agent@123");
        }

        if (!await context.Destinations.AnyAsync())
        {
            context.Destinations.AddRange(
                new Destination { Name = "Paris", Country = "France", Description = "The City of Light", ImageUrl = "https://images.unsplash.com/photo-1502602898657-3e91760cbb34?w=800" },
                new Destination { Name = "Tokyo", Country = "Japan", Description = "Modern metropolis meets ancient tradition", ImageUrl = "https://images.unsplash.com/photo-1540959733332-eab4deabeeaf?w=800" },
                new Destination { Name = "Bali", Country = "Indonesia", Description = "Tropical paradise with rich culture", ImageUrl = "https://images.unsplash.com/photo-1537996194471-e657df975ab4?w=800" },
                new Destination { Name = "New York", Country = "USA", Description = "The city that never sleeps", ImageUrl = "https://images.unsplash.com/photo-1496442226666-8d0d0e62e056?w=800" },
                new Destination { Name = "Rome", Country = "Italy", Description = "Eternal city of history and cuisine", ImageUrl = "https://images.unsplash.com/photo-1552832230-c0197dd311b5?w=800" });
            await context.SaveChangesAsync();
        }

        var owner = await userManager.FindByEmailAsync("owner@traveladvisor.com");
        if (owner is not null && !await context.Hotels.AnyAsync())
        {
            var parisHotel = new Hotel
            {
                OwnerId = owner.Id,
                Name = "Le Grand Paris Hotel",
                Address = "123 Champs-Élysées",
                City = "Paris",
                Country = "France",
                Description = "Luxury hotel in the heart of Paris"
            };
            var tokyoHotel = new Hotel
            {
                OwnerId = owner.Id,
                Name = "Sakura Inn Tokyo",
                Address = "45 Shibuya Street",
                City = "Tokyo",
                Country = "Japan",
                Description = "Modern hotel near Shibuya crossing"
            };
            context.Hotels.AddRange(parisHotel, tokyoHotel);
            await context.SaveChangesAsync();

            context.Rooms.AddRange(
                new Room { HotelId = parisHotel.Id, Name = "Deluxe Suite", RoomType = "Suite", PricePerNight = 250, Capacity = 2, IsAvailable = true },
                new Room { HotelId = parisHotel.Id, Name = "Standard Double", RoomType = "Double", PricePerNight = 120, Capacity = 2, IsAvailable = true },
                new Room { HotelId = tokyoHotel.Id, Name = "City View Room", RoomType = "Standard", PricePerNight = 150, Capacity = 2, IsAvailable = true },
                new Room { HotelId = tokyoHotel.Id, Name = "Family Room", RoomType = "Family", PricePerNight = 200, Capacity = 4, IsAvailable = true });
            await context.SaveChangesAsync();
        }

        var agent = await userManager.FindByEmailAsync("agent@traveladvisor.com");
        if (agent is not null && !await context.TravelPackages.AnyAsync())
        {
            var paris = await context.Destinations.FirstAsync(d => d.Name == "Paris");
            var bali = await context.Destinations.FirstAsync(d => d.Name == "Bali");
            var tokyo = await context.Destinations.FirstAsync(d => d.Name == "Tokyo");

            var parisPkg = new TravelPackage
            {
                AgentId = agent.Id,
                DestinationId = paris.Id,
                Title = "Paris Romance Getaway",
                Description = "5 days exploring Paris including Eiffel Tower, Louvre, and Seine cruise",
                Price = 1299,
                DurationDays = 5
            };
            var baliPkg = new TravelPackage
            {
                AgentId = agent.Id,
                DestinationId = bali.Id,
                Title = "Bali Adventure Package",
                Description = "7 days of beaches, temples, and rice terraces",
                Price = 899,
                DurationDays = 7
            };
            var tokyoPkg = new TravelPackage
            {
                AgentId = agent.Id,
                DestinationId = tokyo.Id,
                Title = "Tokyo Explorer",
                Description = "4 days discovering Tokyo's highlights",
                Price = 749,
                DurationDays = 4
            };
            context.TravelPackages.AddRange(parisPkg, baliPkg, tokyoPkg);
            await context.SaveChangesAsync();

            context.PackageActivities.AddRange(
                new PackageActivity { TravelPackageId = parisPkg.Id, Title = "Eiffel Tower Visit", Description = "Skip-the-line tickets", DayNumber = 1, Price = 50, SortOrder = 1 },
                new PackageActivity { TravelPackageId = parisPkg.Id, Title = "Seine River Cruise", Description = "Evening cruise with dinner", DayNumber = 2, Price = 80, SortOrder = 2 },
                new PackageActivity { TravelPackageId = parisPkg.Id, Title = "Louvre Museum Tour", Description = "Guided tour of masterpieces", DayNumber = 3, Price = 60, SortOrder = 3 },
                new PackageActivity { TravelPackageId = baliPkg.Id, Title = "Ubud Rice Terrace Trek", Description = "Half-day guided trek", DayNumber = 2, Price = 40, SortOrder = 1 },
                new PackageActivity { TravelPackageId = baliPkg.Id, Title = "Temple Sunset Tour", Description = "Visit Tanah Lot at sunset", DayNumber = 4, Price = 35, SortOrder = 2 },
                new PackageActivity { TravelPackageId = tokyoPkg.Id, Title = "Shibuya Food Tour", Description = "Street food tasting experience", DayNumber = 1, Price = 45, SortOrder = 1 },
                new PackageActivity { TravelPackageId = tokyoPkg.Id, Title = "Mt. Fuji Day Trip", Description = "Full day excursion", DayNumber = 3, Price = 120, SortOrder = 2 });
            await context.SaveChangesAsync();
        }
    }
}
