using TravelAdvisor.Core.Entities;
using TravelAdvisor.Core.Enums;

namespace TravelAdvisor.Core.Interfaces.Repositories;

public sealed class HotelSearchCriteria
{
    public string? Query { get; init; }
    public string? City { get; init; }
    public string? Country { get; init; }
    public ApprovalStatus? ApprovalStatus { get; init; }
    public bool ApprovedOnly { get; init; } = true;
    public decimal? MaxPricePerNight { get; init; }
    public int? Guests { get; init; }
}

public sealed class PackageSearchCriteria
{
    public string? Query { get; init; }
    public int? DestinationId { get; init; }
    public ApprovalStatus? ApprovalStatus { get; init; }
    public bool ApprovedOnly { get; init; } = true;
    public decimal? MaxPrice { get; init; }
    public int? MaxDurationDays { get; init; }
}

public interface IHotelRepository
{
    Task<List<Hotel>> SearchAsync(HotelSearchCriteria criteria, CancellationToken cancellationToken = default);
    Task<List<Hotel>> ListByOwnerAsync(string ownerId, CancellationToken cancellationToken = default);
    Task<Hotel?> GetByIdAsync(int id, bool includeRooms = false, CancellationToken cancellationToken = default);
    Task<bool> HasBookingsAsync(int hotelId, CancellationToken cancellationToken = default);
    void Add(Hotel hotel);
    void Remove(Hotel hotel);
}

public interface IRoomRepository
{
    Task<Room?> GetAsync(int hotelId, int roomId, CancellationToken cancellationToken = default);
    Task<Room?> GetByIdAsync(int roomId, CancellationToken cancellationToken = default);
    Task<List<Room>> ListByHotelAsync(int hotelId, CancellationToken cancellationToken = default);
    Task<bool> HasBookingsAsync(int roomId, CancellationToken cancellationToken = default);
    void Add(Room room);
    void Remove(Room room);
}

public interface IPackageRepository
{
    Task<List<TravelPackage>> SearchAsync(PackageSearchCriteria criteria, CancellationToken cancellationToken = default);
    Task<List<TravelPackage>> ListByAgentAsync(string agentId, CancellationToken cancellationToken = default);
    Task<TravelPackage?> GetByIdAsync(int id, bool includeDetails = true, CancellationToken cancellationToken = default);
    Task<PackageActivity?> GetActivityAsync(int packageId, int activityId, CancellationToken cancellationToken = default);
    Task<bool> HasBookingsAsync(int packageId, CancellationToken cancellationToken = default);
    void Add(TravelPackage package);
    void Remove(TravelPackage package);
    void AddActivity(PackageActivity activity);
    void RemoveActivity(PackageActivity activity);
}

public interface IDestinationRepository
{
    Task<List<Destination>> ListAsync(string? query = null, CancellationToken cancellationToken = default);
    Task<Destination?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> NameExistsAsync(string name, string country, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<int> CountPackagesAsync(int destinationId, CancellationToken cancellationToken = default);
    void Add(Destination destination);
    void Remove(Destination destination);
}
