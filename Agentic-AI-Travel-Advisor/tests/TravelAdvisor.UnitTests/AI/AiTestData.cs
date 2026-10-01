using TravelAdvisor.Core.DTOs.AI;
using TravelAdvisor.Core.Enums;

namespace TravelAdvisor.UnitTests.AI;

/// <summary>A small Ella catalog mirroring the demo seed (prices in LKR).</summary>
internal static class AiTestData
{
    public static CatalogDestinationMatch Ella => new() { Id = 6, Name = "Ella", Country = "Sri Lanka", Description = "Hill country town" };

    public static List<CatalogHotelMatch> EllaRooms() =>
    [
        new() { HotelId = 3, HotelName = "Ella Gap View Inn", City = "Ella", Country = "Sri Lanka", RoomId = 5, RoomName = "Garden Double", PricePerNight = 8000, Capacity = 2 },
        new() { HotelId = 3, HotelName = "Ella Gap View Inn", City = "Ella", Country = "Sri Lanka", RoomId = 6, RoomName = "Family Room", PricePerNight = 12000, Capacity = 4 },
        new() { HotelId = 4, HotelName = "Ella Peak Resort", City = "Ella", Country = "Sri Lanka", RoomId = 7, RoomName = "Deluxe Suite", PricePerNight = 30000, Capacity = 2, AverageRating = 4.8 }
    ];

    /// <summary>Base 28,000 + 8,000 activity = 36,000 per person; 72,000 for two (the DEF-007 trap).</summary>
    public static List<CatalogPackageMatch> EllaPackages() =>
    [
        new()
        {
            Id = 7, Title = "Ella Hiking Escape", DestinationId = 6, DestinationName = "Ella", Price = 28000, DurationDays = 2, MaxTravelers = 10,
            Activities =
            [
                new() { Id = 1, TravelPackageId = 7, Title = "Little Adam's Peak hike", Category = "hiking", DayNumber = 1, Price = 0 },
                new() { Id = 2, TravelPackageId = 7, Title = "Ravana Falls zipline", Category = "adventure", DayNumber = 2, Price = 8000 }
            ]
        }
    ];

    public static List<CatalogActivityMatch> EllaActivities() =>
    [
        new() { Id = 1, TravelPackageId = 7, PackageTitle = "Ella Hiking Escape", DestinationName = "Ella", Title = "Little Adam's Peak hike", Category = "hiking", DayNumber = 1, Price = 0 },
        new() { Id = 2, TravelPackageId = 7, PackageTitle = "Ella Hiking Escape", DestinationName = "Ella", Title = "Ravana Falls zipline", Category = "adventure", DayNumber = 2, Price = 8000 }
    ];

    public static List<CatalogTransportMatch> EllaTransport() =>
    [
        new() { Id = 1, Mode = TransportMode.Train, FromLocation = "Kandy", ToLocation = "Ella", DestinationId = 6, DepartureTime = new TimeSpan(8, 47, 0), DurationMinutes = 420, PricePerPerson = 2000, Capacity = 100 },
        new() { Id = 2, Mode = TransportMode.TukTuk, FromLocation = "Ella town", ToLocation = "Nine Arch Bridge", DestinationId = 6, DurationMinutes = 15, PricePerPerson = 500, Capacity = 3 }
    ];
}
