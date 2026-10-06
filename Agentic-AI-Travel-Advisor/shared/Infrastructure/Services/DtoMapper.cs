using TravelAdvisor.Core.DTOs.Auth;
using TravelAdvisor.Core.DTOs.Bookings;
using TravelAdvisor.Core.DTOs.Destinations;
using TravelAdvisor.Core.DTOs.Hotels;
using TravelAdvisor.Core.DTOs.Itineraries;
using TravelAdvisor.Core.DTOs.Packages;
using TravelAdvisor.Core.DTOs.Users;
using TravelAdvisor.Core.Entities;

namespace TravelAdvisor.Infrastructure.Services;

internal static class DtoMapper
{
    public static UserDto ToDto(this ApplicationUser user) => new()
    {
        Id = user.Id,
        Email = user.Email ?? string.Empty,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Role = user.Role?.Name ?? string.Empty,
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt
    };

    public static TravelPreferencesDto ToDto(this TravelPreferences? prefs) => new()
    {
        BudgetMin = prefs?.BudgetMin,
        BudgetMax = prefs?.BudgetMax,
        PreferredClimate = prefs?.PreferredClimate,
        Interests = prefs?.Interests,
        AccommodationPreference = prefs?.AccommodationPreference,
        TransportPreference = prefs?.TransportPreference
    };

    public static HotelDto ToDto(this Hotel hotel) => Fill(new HotelDto(), hotel);

    public static HotelDetailDto ToDetailDto(this Hotel hotel)
    {
        var dto = Fill(new HotelDetailDto(), hotel);
        dto.Rooms = hotel.Rooms.OrderBy(r => r.Name).Select(r => r.ToDto()).ToList();
        return dto;
    }

    private static T Fill<T>(T dto, Hotel hotel) where T : HotelDto
    {
        dto.Id = hotel.Id;
        dto.Name = hotel.Name;
        dto.Address = hotel.Address;
        dto.City = hotel.City;
        dto.Country = hotel.Country;
        dto.Description = hotel.Description;
        dto.ImageUrl = hotel.ImageUrl;
        dto.RoomCount = hotel.Rooms.Count;
        dto.ApprovalStatus = hotel.ApprovalStatus;
        dto.MinPricePerNight = hotel.Rooms.Count == 0 ? null : hotel.Rooms.Min(r => r.PricePerNight);
        dto.ReviewCount = hotel.Reviews.Count;
        dto.AverageRating = hotel.Reviews.Count == 0 ? null : Math.Round(hotel.Reviews.Average(r => r.Rating), 1);
        return dto;
    }

    public static RoomDto ToDto(this Room room) => new()
    {
        Id = room.Id,
        HotelId = room.HotelId,
        Name = room.Name,
        RoomType = room.RoomType,
        PricePerNight = room.PricePerNight,
        Capacity = room.Capacity,
        IsAvailable = room.IsAvailable
    };

    public static TravelPackageDto ToDto(this TravelPackage package) => Fill(new TravelPackageDto(), package);

    public static TravelPackageDetailDto ToDetailDto(this TravelPackage package)
    {
        var dto = Fill(new TravelPackageDetailDto(), package);
        dto.Activities = package.Activities
            .OrderBy(a => a.DayNumber).ThenBy(a => a.SortOrder)
            .Select(a => a.ToDto())
            .ToList();
        return dto;
    }

    private static T Fill<T>(T dto, TravelPackage package) where T : TravelPackageDto
    {
        dto.Id = package.Id;
        dto.Title = package.Title;
        dto.Description = package.Description;
        dto.Price = package.Price;
        dto.DurationDays = package.DurationDays;
        dto.DestinationId = package.DestinationId;
        dto.DestinationName = package.Destination?.Name ?? string.Empty;
        dto.DestinationCountry = package.Destination?.Country ?? string.Empty;
        dto.ImageUrl = string.IsNullOrWhiteSpace(package.ImageUrl) ? package.Destination?.ImageUrl : package.ImageUrl;
        dto.ActivityCount = package.Activities.Count;
        dto.ApprovalStatus = package.ApprovalStatus;
        dto.TotalPrice = package.Price + package.Activities.Sum(a => a.Price);
        dto.MaxTravelers = package.MaxTravelers;
        dto.ReviewCount = package.Reviews.Count;
        dto.AverageRating = package.Reviews.Count == 0 ? null : Math.Round(package.Reviews.Average(r => r.Rating), 1);
        return dto;
    }

    public static PackageActivityDto ToDto(this PackageActivity activity) => new()
    {
        Id = activity.Id,
        TravelPackageId = activity.TravelPackageId,
        Title = activity.Title,
        Description = activity.Description,
        DayNumber = activity.DayNumber,
        Price = activity.Price,
        SortOrder = activity.SortOrder
    };

    public static DestinationDto ToDto(this Destination destination) => new()
    {
        Id = destination.Id,
        Name = destination.Name,
        Country = destination.Country,
        Description = destination.Description,
        ImageUrl = destination.ImageUrl
    };

    public static BookingDto ToDto(this Booking booking) => new()
    {
        Id = booking.Id,
        UserId = booking.UserId,
        RoomId = booking.RoomId,
        TravelPackageId = booking.TravelPackageId,
        HotelId = booking.Room?.HotelId,
        CheckIn = booking.CheckIn,
        CheckOut = booking.CheckOut,
        Guests = booking.Guests,
        Notes = booking.Notes,
        Status = booking.Status,
        TotalPrice = booking.TotalPrice,
        CreatedAt = booking.CreatedAt,
        CancelledAt = booking.CancelledAt,
        RoomName = booking.Room?.Name,
        HotelName = booking.Room?.Hotel?.Name,
        PackageTitle = booking.TravelPackage?.Title,
        UserEmail = booking.User?.Email
    };

    public static ItineraryDto ToDto(this Itinerary itinerary) => Fill(new ItineraryDto(), itinerary);

    public static ItineraryDetailDto ToDetailDto(this Itinerary itinerary)
    {
        var dto = Fill(new ItineraryDetailDto(), itinerary);
        dto.Items = itinerary.Items
            .OrderBy(i => i.DayNumber).ThenBy(i => i.SortOrder)
            .Select(i => new ItineraryItemDto
            {
                Id = i.Id,
                DayNumber = i.DayNumber,
                Title = i.Title,
                Description = i.Description,
                StartTime = i.StartTime,
                SortOrder = i.SortOrder
            })
            .ToList();
        return dto;
    }

    private static T Fill<T>(T dto, Itinerary itinerary) where T : ItineraryDto
    {
        dto.Id = itinerary.Id;
        dto.Title = itinerary.Title;
        dto.StartDate = itinerary.StartDate;
        dto.EndDate = itinerary.EndDate;
        dto.Status = (int)itinerary.Status;
        dto.EstimatedCost = itinerary.EstimatedCost;
        dto.Budget = itinerary.Budget;
        dto.Travelers = itinerary.Travelers;
        dto.DestinationId = itinerary.DestinationId;
        dto.DestinationName = itinerary.Destination?.Name;
        dto.ConversationId = itinerary.ConversationId;
        dto.Summary = itinerary.Summary;
        dto.ItemCount = itinerary.Items.Count;
        return dto;
    }
}
