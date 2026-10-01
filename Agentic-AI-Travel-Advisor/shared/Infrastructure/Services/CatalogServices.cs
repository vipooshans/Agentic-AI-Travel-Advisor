using TravelAdvisor.Core.Common;
using TravelAdvisor.Core.DTOs.Destinations;
using TravelAdvisor.Core.DTOs.Hotels;
using TravelAdvisor.Core.DTOs.Packages;
using TravelAdvisor.Core.Entities;
using TravelAdvisor.Core.Enums;
using TravelAdvisor.Core.Interfaces.Repositories;
using TravelAdvisor.Core.Interfaces.Services;

namespace TravelAdvisor.Infrastructure.Services;

internal static class Ownership
{
    public static void EnsureOwnerOrAdmin(UserContext caller, string ownerId)
    {
        if (!caller.IsAdmin && caller.UserId != ownerId)
            throw new ForbiddenException();
    }

    public static bool CanView(UserContext? caller, ApprovalStatus status, string ownerId) =>
        status == ApprovalStatus.Approved || caller is not null && (caller.IsAdmin || caller.UserId == ownerId);

    public static void EnsureApprovalDecision(ApprovalStatus status)
    {
        if (status is not ApprovalStatus.Approved and not ApprovalStatus.Rejected)
            throw new BusinessRuleException("Status must be Approved or Rejected.");
    }

    public static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class HotelService(IHotelRepository hotels, IUnitOfWork unitOfWork) : IHotelService
{
    public async Task<List<HotelDto>> SearchAsync(UserContext? caller, HotelSearchQuery query, CancellationToken cancellationToken = default)
    {
        var isAdmin = caller?.IsAdmin == true;
        var results = await hotels.SearchAsync(new HotelSearchCriteria
        {
            Query = query.Q,
            City = query.City,
            Country = query.Country,
            ApprovedOnly = !isAdmin,
            ApprovalStatus = isAdmin ? query.ApprovalStatus : null,
            MaxPricePerNight = query.MaxPrice,
            Guests = query.Guests
        }, cancellationToken);
        return results.Select(h => h.ToDto()).ToList();
    }

    public async Task<List<HotelDto>> ListMineAsync(UserContext caller, CancellationToken cancellationToken = default) =>
        (await hotels.ListByOwnerAsync(caller.UserId, cancellationToken)).Select(h => h.ToDto()).ToList();

    public async Task<HotelDetailDto> GetAsync(UserContext? caller, int id, CancellationToken cancellationToken = default)
    {
        var hotel = await hotels.GetByIdAsync(id, includeRooms: true, cancellationToken);
        if (hotel is null || !Ownership.CanView(caller, hotel.ApprovalStatus, hotel.OwnerId))
            throw new NotFoundException("Hotel not found.");
        return hotel.ToDetailDto();
    }

    public async Task<HotelDto> CreateAsync(UserContext caller, CreateHotelRequest request, CancellationToken cancellationToken = default)
    {
        var hotel = new Hotel { OwnerId = caller.UserId, ApprovalStatus = ApprovalStatus.Pending };
        Apply(hotel, request.Name, request.Address, request.City, request.Country, request.Description, request.ImageUrl);
        hotels.Add(hotel);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return hotel.ToDto();
    }

    public async Task<HotelDto> UpdateAsync(UserContext caller, int id, UpdateHotelRequest request, CancellationToken cancellationToken = default)
    {
        var hotel = await hotels.GetByIdAsync(id, includeRooms: true, cancellationToken)
                    ?? throw new NotFoundException("Hotel not found.");
        Ownership.EnsureOwnerOrAdmin(caller, hotel.OwnerId);

        Apply(hotel, request.Name, request.Address, request.City, request.Country, request.Description, request.ImageUrl);
        if (hotel.ApprovalStatus == ApprovalStatus.Rejected)
            hotel.ApprovalStatus = ApprovalStatus.Pending;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return hotel.ToDto();
    }

    public async Task DeleteAsync(UserContext caller, int id, CancellationToken cancellationToken = default)
    {
        var hotel = await hotels.GetByIdAsync(id, cancellationToken: cancellationToken)
                    ?? throw new NotFoundException("Hotel not found.");
        Ownership.EnsureOwnerOrAdmin(caller, hotel.OwnerId);

        if (await hotels.HasBookingsAsync(id, cancellationToken))
            throw new ConflictException("This hotel has bookings and cannot be deleted. Mark its rooms unavailable instead.");

        hotels.Remove(hotel);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<HotelDto> SetApprovalAsync(int id, UpdateApprovalRequest request, CancellationToken cancellationToken = default)
    {
        Ownership.EnsureApprovalDecision(request.Status);
        var hotel = await hotels.GetByIdAsync(id, includeRooms: true, cancellationToken)
                    ?? throw new NotFoundException("Hotel not found.");
        hotel.ApprovalStatus = request.Status;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return hotel.ToDto();
    }

    private static void Apply(Hotel hotel, string name, string address, string city, string country, string? description, string? imageUrl)
    {
        hotel.Name = name.Trim();
        hotel.Address = address.Trim();
        hotel.City = city.Trim();
        hotel.Country = country.Trim();
        hotel.Description = Ownership.Clean(description);
        hotel.ImageUrl = Ownership.Clean(imageUrl) ?? hotel.ImageUrl;
    }
}

public sealed class RoomService(IHotelRepository hotels, IRoomRepository rooms, IUnitOfWork unitOfWork) : IRoomService
{
    public async Task<List<RoomDto>> ListAsync(UserContext? caller, int hotelId, CancellationToken cancellationToken = default)
    {
        var hotel = await hotels.GetByIdAsync(hotelId, cancellationToken: cancellationToken);
        if (hotel is null || !Ownership.CanView(caller, hotel.ApprovalStatus, hotel.OwnerId))
            throw new NotFoundException("Hotel not found.");
        return (await rooms.ListByHotelAsync(hotelId, cancellationToken)).Select(r => r.ToDto()).ToList();
    }

    public async Task<RoomDto> CreateAsync(UserContext caller, int hotelId, CreateRoomRequest request, CancellationToken cancellationToken = default)
    {
        var hotel = await hotels.GetByIdAsync(hotelId, cancellationToken: cancellationToken)
                    ?? throw new NotFoundException("Hotel not found.");
        Ownership.EnsureOwnerOrAdmin(caller, hotel.OwnerId);

        var room = new Room
        {
            HotelId = hotelId,
            Name = request.Name.Trim(),
            RoomType = request.RoomType.Trim(),
            PricePerNight = request.PricePerNight,
            Capacity = request.Capacity,
            IsAvailable = true
        };
        rooms.Add(room);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return room.ToDto();
    }

    public async Task<RoomDto> UpdateAsync(UserContext caller, int hotelId, int roomId, UpdateRoomRequest request, CancellationToken cancellationToken = default)
    {
        var room = await GetOwnedRoomAsync(caller, hotelId, roomId, cancellationToken);
        room.Name = request.Name.Trim();
        room.RoomType = request.RoomType.Trim();
        room.PricePerNight = request.PricePerNight;
        room.Capacity = request.Capacity;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return room.ToDto();
    }

    public async Task<RoomDto> SetAvailabilityAsync(UserContext caller, int hotelId, int roomId, bool isAvailable, CancellationToken cancellationToken = default)
    {
        var room = await GetOwnedRoomAsync(caller, hotelId, roomId, cancellationToken);
        room.IsAvailable = isAvailable;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return room.ToDto();
    }

    public async Task DeleteAsync(UserContext caller, int hotelId, int roomId, CancellationToken cancellationToken = default)
    {
        var room = await GetOwnedRoomAsync(caller, hotelId, roomId, cancellationToken);
        if (await rooms.HasBookingsAsync(roomId, cancellationToken))
            throw new ConflictException("This room has bookings and cannot be deleted. Mark it unavailable instead.");
        rooms.Remove(room);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Room> GetOwnedRoomAsync(UserContext caller, int hotelId, int roomId, CancellationToken cancellationToken)
    {
        var room = await rooms.GetAsync(hotelId, roomId, cancellationToken) ?? throw new NotFoundException("Room not found.");
        Ownership.EnsureOwnerOrAdmin(caller, room.Hotel.OwnerId);
        return room;
    }
}

public sealed class PackageService(
    IPackageRepository packages,
    IDestinationRepository destinations,
    IUnitOfWork unitOfWork) : IPackageService
{
    public async Task<List<TravelPackageDto>> SearchAsync(UserContext? caller, PackageSearchQuery query, CancellationToken cancellationToken = default)
    {
        var isAdmin = caller?.IsAdmin == true;
        var results = await packages.SearchAsync(new PackageSearchCriteria
        {
            Query = query.Q,
            DestinationId = query.DestinationId,
            ApprovedOnly = !isAdmin,
            ApprovalStatus = isAdmin ? query.ApprovalStatus : null,
            MaxPrice = query.MaxPrice,
            MaxDurationDays = query.MaxDurationDays
        }, cancellationToken);
        return results.Select(p => p.ToDto()).ToList();
    }

    public async Task<List<TravelPackageDto>> ListMineAsync(UserContext caller, CancellationToken cancellationToken = default) =>
        (await packages.ListByAgentAsync(caller.UserId, cancellationToken)).Select(p => p.ToDto()).ToList();

    public async Task<TravelPackageDetailDto> GetAsync(UserContext? caller, int id, CancellationToken cancellationToken = default)
    {
        var package = await packages.GetByIdAsync(id, cancellationToken: cancellationToken);
        if (package is null || !Ownership.CanView(caller, package.ApprovalStatus, package.AgentId))
            throw new NotFoundException("Package not found.");
        return package.ToDetailDto();
    }

    public async Task<TravelPackageDto> CreateAsync(UserContext caller, CreatePackageRequest request, CancellationToken cancellationToken = default)
    {
        var destination = await destinations.GetByIdAsync(request.DestinationId, cancellationToken)
                          ?? throw new BusinessRuleException("Destination not found.");

        var package = new TravelPackage
        {
            AgentId = caller.UserId,
            DestinationId = destination.Id,
            Destination = destination,
            ApprovalStatus = ApprovalStatus.Pending
        };
        Apply(package, request.Title, request.Description, request.Price, request.DurationDays, request.ImageUrl, request.MaxTravelers);
        packages.Add(package);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return package.ToDto();
    }

    public async Task<TravelPackageDto> UpdateAsync(UserContext caller, int id, UpdatePackageRequest request, CancellationToken cancellationToken = default)
    {
        var package = await packages.GetByIdAsync(id, cancellationToken: cancellationToken)
                      ?? throw new NotFoundException("Package not found.");
        Ownership.EnsureOwnerOrAdmin(caller, package.AgentId);

        if (package.DestinationId != request.DestinationId)
        {
            package.Destination = await destinations.GetByIdAsync(request.DestinationId, cancellationToken)
                                  ?? throw new BusinessRuleException("Destination not found.");
            package.DestinationId = request.DestinationId;
        }

        Apply(package, request.Title, request.Description, request.Price, request.DurationDays, request.ImageUrl, request.MaxTravelers);
        if (package.ApprovalStatus == ApprovalStatus.Rejected)
            package.ApprovalStatus = ApprovalStatus.Pending;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return package.ToDto();
    }

    public async Task DeleteAsync(UserContext caller, int id, CancellationToken cancellationToken = default)
    {
        var package = await packages.GetByIdAsync(id, includeDetails: false, cancellationToken)
                      ?? throw new NotFoundException("Package not found.");
        Ownership.EnsureOwnerOrAdmin(caller, package.AgentId);

        if (await packages.HasBookingsAsync(id, cancellationToken))
            throw new ConflictException("This package has bookings and cannot be deleted.");

        packages.Remove(package);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<PackageActivityDto> AddActivityAsync(UserContext caller, int packageId, CreateActivityRequest request, CancellationToken cancellationToken = default)
    {
        var package = await packages.GetByIdAsync(packageId, includeDetails: false, cancellationToken)
                      ?? throw new NotFoundException("Package not found.");
        Ownership.EnsureOwnerOrAdmin(caller, package.AgentId);

        if (request.DayNumber > package.DurationDays)
            throw new BusinessRuleException(
                $"Day number must be between 1 and {package.DurationDays} for this package.",
                new Dictionary<string, string[]> { ["dayNumber"] = [$"Must be between 1 and {package.DurationDays}."] });

        var activity = new PackageActivity
        {
            TravelPackageId = packageId,
            Title = request.Title.Trim(),
            Description = Ownership.Clean(request.Description),
            DayNumber = request.DayNumber,
            Price = request.Price,
            SortOrder = request.SortOrder
        };
        packages.AddActivity(activity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return activity.ToDto();
    }

    public async Task DeleteActivityAsync(UserContext caller, int packageId, int activityId, CancellationToken cancellationToken = default)
    {
        var activity = await packages.GetActivityAsync(packageId, activityId, cancellationToken)
                       ?? throw new NotFoundException("Activity not found.");
        Ownership.EnsureOwnerOrAdmin(caller, activity.TravelPackage.AgentId);
        packages.RemoveActivity(activity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<TravelPackageDto> SetApprovalAsync(int id, UpdateApprovalRequest request, CancellationToken cancellationToken = default)
    {
        Ownership.EnsureApprovalDecision(request.Status);
        var package = await packages.GetByIdAsync(id, cancellationToken: cancellationToken)
                      ?? throw new NotFoundException("Package not found.");
        package.ApprovalStatus = request.Status;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return package.ToDto();
    }

    private static void Apply(TravelPackage package, string title, string? description, decimal price, int durationDays, string? imageUrl, int maxTravelers)
    {
        package.Title = title.Trim();
        package.Description = Ownership.Clean(description);
        package.Price = price;
        package.DurationDays = durationDays;
        package.ImageUrl = Ownership.Clean(imageUrl) ?? package.ImageUrl;
        package.MaxTravelers = maxTravelers;
    }
}

public sealed class DestinationService(IDestinationRepository destinations, IUnitOfWork unitOfWork) : IDestinationService
{
    public async Task<List<DestinationDto>> ListAsync(string? query, CancellationToken cancellationToken = default) =>
        (await destinations.ListAsync(query, cancellationToken)).Select(d => d.ToDto()).ToList();

    public async Task<DestinationDetailDto> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var destination = await destinations.GetByIdAsync(id, cancellationToken)
                          ?? throw new NotFoundException("Destination not found.");
        return new DestinationDetailDto
        {
            Id = destination.Id,
            Name = destination.Name,
            Country = destination.Country,
            Description = destination.Description,
            ImageUrl = destination.ImageUrl,
            PackageCount = await destinations.CountPackagesAsync(id, cancellationToken)
        };
    }

    public async Task<DestinationDto> CreateAsync(SaveDestinationRequest request, CancellationToken cancellationToken = default)
    {
        if (await destinations.NameExistsAsync(request.Name, request.Country, cancellationToken: cancellationToken))
            throw new ConflictException("A destination with this name already exists in that country.");

        var destination = new Destination();
        Apply(destination, request);
        destinations.Add(destination);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return destination.ToDto();
    }

    public async Task<DestinationDto> UpdateAsync(int id, SaveDestinationRequest request, CancellationToken cancellationToken = default)
    {
        var destination = await destinations.GetByIdAsync(id, cancellationToken)
                          ?? throw new NotFoundException("Destination not found.");
        if (await destinations.NameExistsAsync(request.Name, request.Country, id, cancellationToken))
            throw new ConflictException("A destination with this name already exists in that country.");

        Apply(destination, request);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return destination.ToDto();
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var destination = await destinations.GetByIdAsync(id, cancellationToken)
                          ?? throw new NotFoundException("Destination not found.");
        if (await destinations.CountPackagesAsync(id, cancellationToken) > 0)
            throw new ConflictException("This destination has travel packages and cannot be deleted.");

        destinations.Remove(destination);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static void Apply(Destination destination, SaveDestinationRequest request)
    {
        destination.Name = request.Name.Trim();
        destination.Country = request.Country.Trim();
        destination.Description = Ownership.Clean(request.Description);
        destination.ImageUrl = Ownership.Clean(request.ImageUrl);
    }
}
