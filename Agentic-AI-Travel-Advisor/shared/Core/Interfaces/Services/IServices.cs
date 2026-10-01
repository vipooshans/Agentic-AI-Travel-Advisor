using TravelAdvisor.Core.Common;
using TravelAdvisor.Core.DTOs.Auth;
using TravelAdvisor.Core.DTOs.Bookings;
using TravelAdvisor.Core.DTOs.Destinations;
using TravelAdvisor.Core.DTOs.Hotels;
using TravelAdvisor.Core.DTOs.Itineraries;
using TravelAdvisor.Core.DTOs.Packages;
using TravelAdvisor.Core.DTOs.Reports;
using TravelAdvisor.Core.DTOs.Users;

namespace TravelAdvisor.Core.Interfaces.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<UserDto> GetCurrentUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<UserDto> UpdateProfileAsync(string userId, UpdateProfileRequest request, CancellationToken cancellationToken = default);
}

public interface IUserService
{
    Task<TravelPreferencesDto> GetPreferencesAsync(string userId, CancellationToken cancellationToken = default);
    Task<TravelPreferencesDto> UpdatePreferencesAsync(string userId, TravelPreferencesDto request, CancellationToken cancellationToken = default);
    Task<List<UserDto>> ListAsync(string? role, CancellationToken cancellationToken = default);
    Task<UserDto> CreateStaffAsync(CreateStaffUserRequest request, CancellationToken cancellationToken = default);
    Task<UserDto> SetActiveAsync(UserContext caller, string userId, bool isActive, CancellationToken cancellationToken = default);
}

public interface IHotelService
{
    Task<List<HotelDto>> SearchAsync(UserContext? caller, HotelSearchQuery query, CancellationToken cancellationToken = default);
    Task<List<HotelDto>> ListMineAsync(UserContext caller, CancellationToken cancellationToken = default);
    Task<HotelDetailDto> GetAsync(UserContext? caller, int id, CancellationToken cancellationToken = default);
    Task<HotelDto> CreateAsync(UserContext caller, CreateHotelRequest request, CancellationToken cancellationToken = default);
    Task<HotelDto> UpdateAsync(UserContext caller, int id, UpdateHotelRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(UserContext caller, int id, CancellationToken cancellationToken = default);
    Task<HotelDto> SetApprovalAsync(int id, UpdateApprovalRequest request, CancellationToken cancellationToken = default);
}

public interface IRoomService
{
    Task<List<RoomDto>> ListAsync(UserContext? caller, int hotelId, CancellationToken cancellationToken = default);
    Task<RoomDto> CreateAsync(UserContext caller, int hotelId, CreateRoomRequest request, CancellationToken cancellationToken = default);
    Task<RoomDto> UpdateAsync(UserContext caller, int hotelId, int roomId, UpdateRoomRequest request, CancellationToken cancellationToken = default);
    Task<RoomDto> SetAvailabilityAsync(UserContext caller, int hotelId, int roomId, bool isAvailable, CancellationToken cancellationToken = default);
    Task DeleteAsync(UserContext caller, int hotelId, int roomId, CancellationToken cancellationToken = default);
}

public interface IPackageService
{
    Task<List<TravelPackageDto>> SearchAsync(UserContext? caller, PackageSearchQuery query, CancellationToken cancellationToken = default);
    Task<List<TravelPackageDto>> ListMineAsync(UserContext caller, CancellationToken cancellationToken = default);
    Task<TravelPackageDetailDto> GetAsync(UserContext? caller, int id, CancellationToken cancellationToken = default);
    Task<TravelPackageDto> CreateAsync(UserContext caller, CreatePackageRequest request, CancellationToken cancellationToken = default);
    Task<TravelPackageDto> UpdateAsync(UserContext caller, int id, UpdatePackageRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(UserContext caller, int id, CancellationToken cancellationToken = default);
    Task<PackageActivityDto> AddActivityAsync(UserContext caller, int packageId, CreateActivityRequest request, CancellationToken cancellationToken = default);
    Task DeleteActivityAsync(UserContext caller, int packageId, int activityId, CancellationToken cancellationToken = default);
    Task<TravelPackageDto> SetApprovalAsync(int id, UpdateApprovalRequest request, CancellationToken cancellationToken = default);
}

public interface IDestinationService
{
    Task<List<DestinationDto>> ListAsync(string? query, CancellationToken cancellationToken = default);
    Task<DestinationDetailDto> GetAsync(int id, CancellationToken cancellationToken = default);
    Task<DestinationDto> CreateAsync(SaveDestinationRequest request, CancellationToken cancellationToken = default);
    Task<DestinationDto> UpdateAsync(int id, SaveDestinationRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public interface IBookingService
{
    Task<BookingDto> CreateAsync(UserContext caller, CreateBookingRequest request, CancellationToken cancellationToken = default);
    Task<List<BookingDto>> ListAsync(UserContext caller, CancellationToken cancellationToken = default);
    Task<BookingDto> GetAsync(UserContext caller, int id, CancellationToken cancellationToken = default);
    Task<BookingDto> UpdateStatusAsync(UserContext caller, int id, UpdateBookingStatusRequest request, CancellationToken cancellationToken = default);
}

public interface IItineraryService
{
    Task<ItineraryDetailDto> CreateAsync(UserContext caller, CreateItineraryRequest request, CancellationToken cancellationToken = default);
    Task<List<ItineraryDto>> ListAsync(UserContext caller, CancellationToken cancellationToken = default);
    Task<ItineraryDetailDto> GetAsync(UserContext caller, int id, CancellationToken cancellationToken = default);
    Task DeleteAsync(UserContext caller, int id, CancellationToken cancellationToken = default);
}

public interface IReportService
{
    Task<ReportSummaryDto> GetSummaryAsync(UserContext caller, CancellationToken cancellationToken = default);
}
