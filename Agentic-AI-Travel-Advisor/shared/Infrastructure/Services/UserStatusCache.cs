using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TravelAdvisor.Infrastructure.Data;

namespace TravelAdvisor.Infrastructure.Services;

public sealed record UserStatus(bool IsActive, string Role);

/// <summary>
/// Short-lived cache of each user's active flag and role, checked on every authenticated request so that
/// deactivated users and role changes take effect without waiting for the JWT to expire.
/// </summary>
public interface IUserStatusCache
{
    Task<UserStatus?> GetAsync(string userId, CancellationToken cancellationToken = default);
    void Invalidate(string userId);
}

public sealed class UserStatusCache(IMemoryCache cache, AppDbContext context) : IUserStatusCache
{
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(30);

    public async Task<UserStatus?> GetAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue(Key(userId), out UserStatus? status))
            return status;

        status = await context.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new UserStatus(u.IsActive, u.Role.Name))
            .FirstOrDefaultAsync(cancellationToken);

        cache.Set(Key(userId), status, Ttl);
        return status;
    }

    public void Invalidate(string userId) => cache.Remove(Key(userId));

    private static string Key(string userId) => $"user-status:{userId}";
}
