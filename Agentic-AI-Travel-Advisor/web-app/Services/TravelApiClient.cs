using System.Net;
using System.Net.Http.Json;
using TravelAdvisor.Core.DTOs.Auth;
using TravelAdvisor.Core.DTOs.Bookings;
using TravelAdvisor.Core.DTOs.Destinations;
using TravelAdvisor.Core.DTOs.Hotels;
using TravelAdvisor.Core.DTOs.Packages;
using TravelAdvisor.Core.DTOs.Reports;
using TravelAdvisor.Core.Enums;

namespace TravelAdvisor.Web.Services;

/// <summary>
/// Typed calls to the Travel Advisor API. A 404 on a read returns null (or an empty list);
/// 401 and 403 throw <see cref="ApiUnauthorizedException"/> / <see cref="ApiForbiddenException"/>,
/// which <c>ApiAuthExceptionFilter</c> turns into redirects. Other failures still throw.
/// </summary>
public class TravelApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;

    public TravelApiClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    private HttpClient Client => _httpClientFactory.CreateClient("TravelAdvisorApi");

    private async Task<T?> GetAsync<T>(string url)
    {
        using var response = Authorized(await Client.GetAsync(url));
        if (response.StatusCode == HttpStatusCode.NotFound) return default;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>();
    }

    private async Task<List<T>> GetListAsync<T>(string url) => await GetAsync<List<T>>(url) ?? [];

    private static HttpResponseMessage Authorized(HttpResponseMessage response) => response.StatusCode switch
    {
        HttpStatusCode.Unauthorized => throw new ApiUnauthorizedException(),
        HttpStatusCode.Forbidden => throw new ApiForbiddenException(),
        _ => response
    };

    private static async Task<T?> ReadIfSuccessAsync<T>(HttpResponseMessage response) =>
        Authorized(response).IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<T>() : default;

    private static bool Succeeded(HttpResponseMessage response) => Authorized(response).IsSuccessStatusCode;

    public Task<List<DestinationDto>> GetDestinationsAsync()
        => GetListAsync<DestinationDto>("/api/destinations");

    public Task<DestinationDetailDto?> GetDestinationAsync(int id)
        => GetAsync<DestinationDetailDto>($"/api/destinations/{id}");

    public async Task<DestinationDto?> CreateDestinationAsync(SaveDestinationRequest request)
        => await ReadIfSuccessAsync<DestinationDto>(await Client.PostAsJsonAsync("/api/destinations", request));

    public async Task<bool> UpdateDestinationAsync(int id, SaveDestinationRequest request)
        => Succeeded(await Client.PutAsJsonAsync($"/api/destinations/{id}", request));

    public Task<List<HotelDto>> GetMyHotelsAsync()
        => GetListAsync<HotelDto>("/api/hotels/mine");

    public Task<List<HotelDto>> GetHotelsAsync(ApprovalStatus? approvalStatus = null)
    {
        var qs = approvalStatus.HasValue ? $"?approvalStatus={(int)approvalStatus.Value}" : "";
        return GetListAsync<HotelDto>($"/api/hotels{qs}");
    }

    public Task<HotelDetailDto?> GetHotelAsync(int id)
        => GetAsync<HotelDetailDto>($"/api/hotels/{id}");

    public async Task<HotelDto?> CreateHotelAsync(CreateHotelRequest request)
        => await ReadIfSuccessAsync<HotelDto>(await Client.PostAsJsonAsync("/api/hotels", request));

    public async Task<bool> UpdateHotelAsync(int id, UpdateHotelRequest request)
        => Succeeded(await Client.PutAsJsonAsync($"/api/hotels/{id}", request));

    public async Task<bool> SetHotelApprovalAsync(int id, ApprovalStatus status)
        => Succeeded(await Client.PatchAsJsonAsync($"/api/hotels/{id}/approval", new UpdateApprovalRequest { Status = status }));

    public Task<List<RoomDto>> GetRoomsAsync(int hotelId)
        => GetListAsync<RoomDto>($"/api/hotels/{hotelId}/rooms");

    public async Task<RoomDto?> CreateRoomAsync(int hotelId, CreateRoomRequest request)
        => await ReadIfSuccessAsync<RoomDto>(await Client.PostAsJsonAsync($"/api/hotels/{hotelId}/rooms", request));

    public async Task<bool> UpdateRoomAsync(int hotelId, int roomId, UpdateRoomRequest request)
        => Succeeded(await Client.PutAsJsonAsync($"/api/hotels/{hotelId}/rooms/{roomId}", request));

    public async Task<bool> ToggleRoomAvailabilityAsync(int hotelId, int roomId, bool isAvailable)
        => Succeeded(await Client.PatchAsJsonAsync($"/api/hotels/{hotelId}/rooms/{roomId}/availability", new UpdateRoomAvailabilityRequest { IsAvailable = isAvailable }));

    public Task<List<TravelPackageDto>> GetMyPackagesAsync()
        => GetListAsync<TravelPackageDto>("/api/packages/mine");

    public Task<List<TravelPackageDto>> GetPackagesAsync(ApprovalStatus? approvalStatus = null)
    {
        var qs = approvalStatus.HasValue ? $"?approvalStatus={(int)approvalStatus.Value}" : "";
        return GetListAsync<TravelPackageDto>($"/api/packages{qs}");
    }

    public Task<TravelPackageDetailDto?> GetPackageAsync(int id)
        => GetAsync<TravelPackageDetailDto>($"/api/packages/{id}");

    public async Task<TravelPackageDto?> CreatePackageAsync(CreatePackageRequest request)
        => await ReadIfSuccessAsync<TravelPackageDto>(await Client.PostAsJsonAsync("/api/packages", request));

    public async Task<bool> UpdatePackageAsync(int id, UpdatePackageRequest request)
        => Succeeded(await Client.PutAsJsonAsync($"/api/packages/{id}", request));

    public async Task<bool> SetPackageApprovalAsync(int id, ApprovalStatus status)
        => Succeeded(await Client.PatchAsJsonAsync($"/api/packages/{id}/approval", new UpdateApprovalRequest { Status = status }));

    public async Task<PackageActivityDto?> AddActivityAsync(int packageId, CreateActivityRequest request)
        => await ReadIfSuccessAsync<PackageActivityDto>(await Client.PostAsJsonAsync($"/api/packages/{packageId}/activities", request));

    public async Task<bool> DeleteActivityAsync(int packageId, int activityId)
        => Succeeded(await Client.DeleteAsync($"/api/packages/{packageId}/activities/{activityId}"));

    public Task<List<BookingDto>> GetBookingsAsync()
        => GetListAsync<BookingDto>("/api/bookings");

    public async Task<bool> UpdateBookingStatusAsync(int id, BookingStatus status)
        => Succeeded(await Client.PatchAsJsonAsync($"/api/bookings/{id}/status", new UpdateBookingStatusRequest { Status = status }));

    public Task<ReportSummaryDto?> GetReportSummaryAsync()
        => GetAsync<ReportSummaryDto>("/api/reports/summary");

    public Task<List<UserDto>> GetUsersAsync(string? role = null)
    {
        var qs = string.IsNullOrWhiteSpace(role) ? "" : $"?role={Uri.EscapeDataString(role)}";
        return GetListAsync<UserDto>($"/api/users{qs}");
    }

    public async Task<UserDto?> CreateStaffUserAsync(CreateStaffUserRequest request)
        => await ReadIfSuccessAsync<UserDto>(await Client.PostAsJsonAsync("/api/users", request));

    public async Task<bool> SetUserActiveAsync(string id, bool isActive)
        => Succeeded(await Client.PatchAsJsonAsync($"/api/users/{id}/active", new UpdateUserActiveRequest { IsActive = isActive }));
}
