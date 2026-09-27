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
                Description = "Luxury hotel in the heart of Paris",
                ApprovalStatus = ApprovalStatus.Approved
            };
            var tokyoHotel = new Hotel
            {
                OwnerId = owner.Id,
                Name = "Sakura Inn Tokyo",
                Address = "45 Shibuya Street",
                City = "Tokyo",
                Country = "Japan",
                Description = "Modern hotel near Shibuya crossing",
                ApprovalStatus = ApprovalStatus.Approved
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
                DurationDays = 5,
                ApprovalStatus = ApprovalStatus.Approved
            };
            var baliPkg = new TravelPackage
            {
                AgentId = agent.Id,
                DestinationId = bali.Id,
                Title = "Bali Adventure Package",
                Description = "7 days of beaches, temples, and rice terraces",
                Price = 899,
                DurationDays = 7,
                ApprovalStatus = ApprovalStatus.Approved
            };
            var tokyoPkg = new TravelPackage
            {
                AgentId = agent.Id,
                DestinationId = tokyo.Id,
                Title = "Tokyo Explorer",
                Description = "4 days discovering Tokyo's highlights",
                Price = 749,
                DurationDays = 4,
                ApprovalStatus = ApprovalStatus.Approved
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

        await SeedSriLankaCatalogAsync(context, owner, agent);
    }

    private static async Task SeedSriLankaCatalogAsync(
        AppDbContext context,
        ApplicationUser? owner,
        ApplicationUser? agent)
    {
        if (!await context.Destinations.AnyAsync(d => d.Name == "Ella"))
        {
            context.Destinations.AddRange(
                new Destination
                {
                    Name = "Ella",
                    Country = "Sri Lanka",
                    Description = "Hill-country village famous for Nine Arch Bridge, Little Adam's Peak, and tea estates",
                    ImageUrl = "https://images.unsplash.com/photo-1566296314637-05c0c0c1c0c1?w=800"
                },
                new Destination
                {
                    Name = "Kandy",
                    Country = "Sri Lanka",
                    Description = "Cultural capital with the Temple of the Tooth and lakeside walks",
                    ImageUrl = "https://images.unsplash.com/photo-1625734220649-0c0c0c0c0c0c?w=800"
                },
                new Destination
                {
                    Name = "Galle",
                    Country = "Sri Lanka",
                    Description = "UNESCO Dutch fort, lighthouse, and southern beaches",
                    ImageUrl = "https://images.unsplash.com/photo-1580881977107-bf99924000b0?w=800"
                });
            await context.SaveChangesAsync();
        }

        if (owner is not null && !await context.Hotels.AnyAsync(h => h.City == "Ella"))
        {
            var ellaInn = new Hotel
            {
                OwnerId = owner.Id,
                Name = "Ella Gap View Inn",
                Address = "Passara Road",
                City = "Ella",
                Country = "Sri Lanka",
                Description = "Budget-friendly inn with views over Ella Gap",
                ApprovalStatus = ApprovalStatus.Approved
            };
            var ellaLodge = new Hotel
            {
                OwnerId = owner.Id,
                Name = "Ella Heights Lodge",
                Address = "Wellness Place, Ella",
                City = "Ella",
                Country = "Sri Lanka",
                Description = "Mid-range lodge near Little Adam's Peak",
                ApprovalStatus = ApprovalStatus.Approved
            };
            var kandyHotel = new Hotel
            {
                OwnerId = owner.Id,
                Name = "Kandy Lake House",
                Address = "Lake Drive",
                City = "Kandy",
                Country = "Sri Lanka",
                Description = "Lakeside stay walking distance from the Temple of the Tooth",
                ApprovalStatus = ApprovalStatus.Approved
            };
            var galleHotel = new Hotel
            {
                OwnerId = owner.Id,
                Name = "Galle Fort Stay",
                Address = "Church Street, Galle Fort",
                City = "Galle",
                Country = "Sri Lanka",
                Description = "Heritage guesthouse inside the Dutch Fort",
                ApprovalStatus = ApprovalStatus.Approved
            };
            context.Hotels.AddRange(ellaInn, ellaLodge, kandyHotel, galleHotel);
            await context.SaveChangesAsync();

            context.Rooms.AddRange(
                new Room { HotelId = ellaInn.Id, Name = "Garden Double", RoomType = "Double", PricePerNight = 8000, Capacity = 2, IsAvailable = true },
                new Room { HotelId = ellaInn.Id, Name = "Family Room", RoomType = "Family", PricePerNight = 12000, Capacity = 4, IsAvailable = true },
                new Room { HotelId = ellaLodge.Id, Name = "Peak View Room", RoomType = "Deluxe", PricePerNight = 15000, Capacity = 2, IsAvailable = true },
                new Room { HotelId = ellaLodge.Id, Name = "Suite", RoomType = "Suite", PricePerNight = 18000, Capacity = 3, IsAvailable = true },
                new Room { HotelId = kandyHotel.Id, Name = "Lake View Double", RoomType = "Double", PricePerNight = 9000, Capacity = 2, IsAvailable = true },
                new Room { HotelId = kandyHotel.Id, Name = "Family Suite", RoomType = "Family", PricePerNight = 14000, Capacity = 4, IsAvailable = true },
                new Room { HotelId = galleHotel.Id, Name = "Fort Double", RoomType = "Double", PricePerNight = 10000, Capacity = 2, IsAvailable = true },
                new Room { HotelId = galleHotel.Id, Name = "Lighthouse Suite", RoomType = "Suite", PricePerNight = 16000, Capacity = 3, IsAvailable = true });
            await context.SaveChangesAsync();
        }

        if (agent is not null && !await context.TravelPackages.AnyAsync(p => p.Title == "Ella Hills Escape"))
        {
            var ella = await context.Destinations.FirstAsync(d => d.Name == "Ella");
            var kandy = await context.Destinations.FirstAsync(d => d.Name == "Kandy");
            var galle = await context.Destinations.FirstAsync(d => d.Name == "Galle");

            var ellaPkg = new TravelPackage
            {
                AgentId = agent.Id,
                DestinationId = ella.Id,
                Title = "Ella Hills Escape",
                Description = "3 days of hill-country hikes, Nine Arch Bridge, and tea estates. Hotel not included.",
                Price = 28000,
                DurationDays = 3,
                ApprovalStatus = ApprovalStatus.Approved
            };
            var kandyPkg = new TravelPackage
            {
                AgentId = agent.Id,
                DestinationId = kandy.Id,
                Title = "Kandy Cultural Weekend",
                Description = "3 days covering the Temple of the Tooth, Peradeniya Gardens, and city walks.",
                Price = 30000,
                DurationDays = 3,
                ApprovalStatus = ApprovalStatus.Approved
            };
            var gallePkg = new TravelPackage
            {
                AgentId = agent.Id,
                DestinationId = galle.Id,
                Title = "Galle Fort Getaway",
                Description = "3 days exploring Galle Fort, Unawatuna beach, and the lighthouse.",
                Price = 29000,
                DurationDays = 3,
                ApprovalStatus = ApprovalStatus.Approved
            };
            context.TravelPackages.AddRange(ellaPkg, kandyPkg, gallePkg);
            await context.SaveChangesAsync();

            context.PackageActivities.AddRange(
                new PackageActivity { TravelPackageId = ellaPkg.Id, Title = "Nine Arch Bridge", Description = "Morning visit timed for the hill-country train", DayNumber = 1, Price = 0, SortOrder = 1 },
                new PackageActivity { TravelPackageId = ellaPkg.Id, Title = "Little Adam's Peak", Description = "Guided hike with Ella Gap views", DayNumber = 1, Price = 2500, SortOrder = 2 },
                new PackageActivity { TravelPackageId = ellaPkg.Id, Title = "Ravana Falls", Description = "Waterfall stop and photo break", DayNumber = 2, Price = 0, SortOrder = 3 },
                new PackageActivity { TravelPackageId = ellaPkg.Id, Title = "Tea Estate Walk", Description = "Factory tour and tasting at a local estate", DayNumber = 2, Price = 3500, SortOrder = 4 },
                new PackageActivity { TravelPackageId = ellaPkg.Id, Title = "Ella Rock Viewpoint", Description = "Optional longer hike or scenic train viewpoint", DayNumber = 3, Price = 2000, SortOrder = 5 },
                new PackageActivity { TravelPackageId = kandyPkg.Id, Title = "Temple of the Tooth", Description = "Guided visit to Sri Dalada Maligawa", DayNumber = 1, Price = 2000, SortOrder = 1 },
                new PackageActivity { TravelPackageId = kandyPkg.Id, Title = "Kandy Lake Walk", Description = "Evening stroll around the lake", DayNumber = 1, Price = 0, SortOrder = 2 },
                new PackageActivity { TravelPackageId = kandyPkg.Id, Title = "Peradeniya Botanical Gardens", Description = "Half-day gardens visit", DayNumber = 2, Price = 3000, SortOrder = 3 },
                new PackageActivity { TravelPackageId = kandyPkg.Id, Title = "Cultural Dance Show", Description = "Traditional Kandyan dance evening", DayNumber = 3, Price = 2500, SortOrder = 4 },
                new PackageActivity { TravelPackageId = gallePkg.Id, Title = "Galle Fort Ramparts", Description = "Sunset walk along the walls to the lighthouse", DayNumber = 1, Price = 0, SortOrder = 1 },
                new PackageActivity { TravelPackageId = gallePkg.Id, Title = "Unawatuna Beach", Description = "Morning swim and cafe stop", DayNumber = 2, Price = 0, SortOrder = 2 },
                new PackageActivity { TravelPackageId = gallePkg.Id, Title = "Maritime Museum", Description = "Dutch-era museum inside the fort", DayNumber = 2, Price = 1500, SortOrder = 3 },
                new PackageActivity { TravelPackageId = gallePkg.Id, Title = "Jungle Beach Walk", Description = "Coastal trail and picnic", DayNumber = 3, Price = 2000, SortOrder = 4 });
            await context.SaveChangesAsync();
        }
    }
}
