using TravelAdvisor.Core.Entities;

namespace TravelAdvisor.Core.Interfaces.Repositories;

public interface IRoleRepository
{
    Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
}

public interface IUserRepository
{
    Task<ApplicationUser?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<List<ApplicationUser>> ListAsync(string? role, CancellationToken cancellationToken = default);
}

public interface ITravelPreferencesRepository
{
    Task<TravelPreferences?> GetByUserAsync(string userId, CancellationToken cancellationToken = default);
    void Add(TravelPreferences preferences);
}

public interface IItineraryRepository
{
    Task<List<Itinerary>> ListByUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<Itinerary?> GetForUserAsync(int id, string userId, CancellationToken cancellationToken = default);
    void Add(Itinerary itinerary);
    void Remove(Itinerary itinerary);
}
