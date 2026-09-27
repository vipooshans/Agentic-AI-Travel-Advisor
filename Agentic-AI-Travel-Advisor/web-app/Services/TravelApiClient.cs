using System.Net.Http.Json;
using TravelAdvisor.Core.DTOs.Auth;
using TravelAdvisor.Core.DTOs.Bookings;
using TravelAdvisor.Core.DTOs.Destinations;
using TravelAdvisor.Core.DTOs.Hotels;
using TravelAdvisor.Core.DTOs.Packages;
using TravelAdvisor.Core.DTOs.Reports;
using TravelAdvisor.Core.Enums;

namespace TravelAdvisor.Web.Services;

public class TravelApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;

    public TravelApiClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    private HttpClient Client => _httpClientFactory.CreateClient("TravelAdvisorApi");

    public async Task<List<DestinationDto>> GetDestinationsAsync()
        => await Client.GetFromJsonAsync<List<DestinationDto>>("/api/destinations") ?? [];

    public async Task<List<HotelDto>> GetMyHotelsAsync()
        => await Client.GetFromJsonAsync<List<HotelDto>>("/api/hotels/mine") ?? [];

    public async Task<List<HotelDto>> GetHotelsAsync(ApprovalStatus? approvalStatus = null)
    {
        var qs = approvalStatus.HasValue ? $"?approvalStatus={(int)approvalStatus.Value}" : "";
        return await Client.GetFromJsonAsync<List<HotelDto>>($"/api/hotels{qs}") ?? [];
    }

    public async Task<HotelDetailDto?> GetHotelAsync(int id)
        => await Client.GetFromJsonAsync<HotelDetailDto>($"/api/hotels/{id}");

    public async Task<HotelDto?> CreateHotelAsync(CreateHotelRequest request)
    {
        var response = await Client.PostAsJsonAsync("/api/hotels", request);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<HotelDto>() : null;
    }

    public async Task<bool> UpdateHotelAsync(int id, UpdateHotelRequest request)
    {
        var response = await Client.PutAsJsonAsync($"/api/hotels/{id}", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> SetHotelApprovalAsync(int id, ApprovalStatus status)
    {
        var response = await Client.PatchAsJsonAsync($"/api/hotels/{id}/approval", new UpdateApprovalRequest { Status = status });
        return response.IsSuccessStatusCode;
    }

    public async Task<List<RoomDto>> GetRoomsAsync(int hotelId)
        => await Client.GetFromJsonAsync<List<RoomDto>>($"/api/hotels/{hotelId}/rooms") ?? [];

    public async Task<RoomDto?> CreateRoomAsync(int hotelId, CreateRoomRequest request)
    {
        var response = await Client.PostAsJsonAsync($"/api/hotels/{hotelId}/rooms", request);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<RoomDto>() : null;
    }

    public async Task<bool> UpdateRoomAsync(int hotelId, int roomId, UpdateRoomRequest request)
    {
        var response = await Client.PutAsJsonAsync($"/api/hotels/{hotelId}/rooms/{roomId}", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> ToggleRoomAvailabilityAsync(int hotelId, int roomId, bool isAvailable)
    {
        var response = await Client.PatchAsJsonAsync($"/api/hotels/{hotelId}/rooms/{roomId}/availability", new UpdateRoomAvailabilityRequest { IsAvailable = isAvailable });
        return response.IsSuccessStatusCode;
    }

    public async Task<List<TravelPackageDto>> GetMyPackagesAsync()
        => await Client.GetFromJsonAsync<List<TravelPackageDto>>("/api/packages/mine") ?? [];

    public async Task<List<TravelPackageDto>> GetPackagesAsync(ApprovalStatus? approvalStatus = null)
    {
        var qs = approvalStatus.HasValue ? $"?approvalStatus={(int)approvalStatus.Value}" : "";
        return await Client.GetFromJsonAsync<List<TravelPackageDto>>($"/api/packages{qs}") ?? [];
    }

    public async Task<TravelPackageDetailDto?> GetPackageAsync(int id)
        => await Client.GetFromJsonAsync<TravelPackageDetailDto>($"/api/packages/{id}");

    public async Task<TravelPackageDto?> CreatePackageAsync(CreatePackageRequest request)
    {
        var response = await Client.PostAsJsonAsync("/api/packages", request);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<TravelPackageDto>() : null;
    }

    public async Task<bool> UpdatePackageAsync(int id, UpdatePackageRequest request)
    {
        var response = await Client.PutAsJsonAsync($"/api/packages/{id}", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> SetPackageApprovalAsync(int id, ApprovalStatus status)
    {
        var response = await Client.PatchAsJsonAsync($"/api/packages/{id}/approval", new UpdateApprovalRequest { Status = status });
        return response.IsSuccessStatusCode;
    }

    public async Task<PackageActivityDto?> AddActivityAsync(int packageId, CreateActivityRequest request)
    {
        var response = await Client.PostAsJsonAsync($"/api/packages/{packageId}/activities", request);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<PackageActivityDto>() : null;
    }

    public async Task<bool> DeleteActivityAsync(int packageId, int activityId)
    {
        var response = await Client.DeleteAsync($"/api/packages/{packageId}/activities/{activityId}");
        return response.IsSuccessStatusCode;
    }

    public async Task<List<BookingDto>> GetBookingsAsync()
        => await Client.GetFromJsonAsync<List<BookingDto>>("/api/bookings") ?? [];

    public async Task<bool> UpdateBookingStatusAsync(int id, BookingStatus status)
    {
        var response = await Client.PatchAsJsonAsync($"/api/bookings/{id}/status", new UpdateBookingStatusRequest { Status = status });
        return response.IsSuccessStatusCode;
    }

    public async Task<ReportSummaryDto?> GetReportSummaryAsync()
        => await Client.GetFromJsonAsync<ReportSummaryDto>("/api/reports/summary");

    public async Task<List<UserDto>> GetUsersAsync(string? role = null)
    {
        var qs = string.IsNullOrWhiteSpace(role) ? "" : $"?role={Uri.EscapeDataString(role)}";
        return await Client.GetFromJsonAsync<List<UserDto>>($"/api/users{qs}") ?? [];
    }

    public async Task<UserDto?> CreateStaffUserAsync(CreateStaffUserRequest request)
    {
        var response = await Client.PostAsJsonAsync("/api/users", request);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<UserDto>() : null;
    }

    public async Task<bool> SetUserActiveAsync(string id, bool isActive)
    {
        var response = await Client.PatchAsJsonAsync($"/api/users/{id}/active", new UpdateUserActiveRequest { IsActive = isActive });
        return response.IsSuccessStatusCode;
    }
}
