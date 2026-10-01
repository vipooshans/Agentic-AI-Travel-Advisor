using Microsoft.EntityFrameworkCore;
using TravelAdvisor.Core.Entities;
using TravelAdvisor.Core.Interfaces.Repositories;
using TravelAdvisor.Infrastructure.Data;

namespace TravelAdvisor.Infrastructure.Repositories;

public sealed class RoleRepository(AppDbContext context) : IRoleRepository
{
    public Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default) =>
        context.AppRoles.FirstOrDefaultAsync(r => r.Name == name, cancellationToken);
}

public sealed class UserRepository(AppDbContext context) : IUserRepository
{
    public Task<ApplicationUser?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<List<ApplicationUser>> ListAsync(string? role, CancellationToken cancellationToken = default)
    {
        var query = context.Users.AsNoTracking().Include(u => u.Role).AsQueryable();
        if (!string.IsNullOrWhiteSpace(role))
            query = query.Where(u => u.Role.Name == role);
        return query.OrderBy(u => u.Email).ToListAsync(cancellationToken);
    }
}

public sealed class TravelPreferencesRepository(AppDbContext context) : ITravelPreferencesRepository
{
    public Task<TravelPreferences?> GetByUserAsync(string userId, CancellationToken cancellationToken = default) =>
        context.TravelPreferences.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

    public void Add(TravelPreferences preferences) => context.TravelPreferences.Add(preferences);
}

public sealed class ItineraryRepository(AppDbContext context) : IItineraryRepository
{
    public Task<List<Itinerary>> ListByUserAsync(string userId, CancellationToken cancellationToken = default) =>
        context.Itineraries.AsNoTracking()
            .Include(i => i.Destination)
            .Include(i => i.Items)
            .Where(i => i.UserId == userId)
            .OrderByDescending(i => i.StartDate)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

    public Task<Itinerary?> GetForUserAsync(int id, string userId, CancellationToken cancellationToken = default) =>
        context.Itineraries
            .Include(i => i.Destination)
            .Include(i => i.Items)
            .AsSplitQuery()
            .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId, cancellationToken);

    public Task<bool> ConversationBelongsToAsync(int conversationId, string userId, CancellationToken cancellationToken = default) =>
        context.AIConversations.AnyAsync(c => c.Id == conversationId && c.UserId == userId, cancellationToken);

    public void Add(Itinerary itinerary) => context.Itineraries.Add(itinerary);

    public void Remove(Itinerary itinerary) => context.Itineraries.Remove(itinerary);
}
