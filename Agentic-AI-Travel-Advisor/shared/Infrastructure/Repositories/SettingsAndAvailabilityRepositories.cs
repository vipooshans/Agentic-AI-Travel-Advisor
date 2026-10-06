using Microsoft.EntityFrameworkCore;
using TravelAdvisor.Core.Entities;
using TravelAdvisor.Core.Interfaces.Repositories;
using TravelAdvisor.Infrastructure.Data;

namespace TravelAdvisor.Infrastructure.Repositories;

public sealed class RoomAvailabilityRepository(AppDbContext context) : IRoomAvailabilityRepository
{
    public Task<List<RoomAvailability>> ListAsync(int roomId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default) =>
        context.RoomAvailability
            .Where(a => a.RoomId == roomId && a.Date >= from && a.Date < to)
            .OrderBy(a => a.Date)
            .ToListAsync(cancellationToken);

    public void Add(RoomAvailability entry) => context.RoomAvailability.Add(entry);

    public void Remove(RoomAvailability entry) => context.RoomAvailability.Remove(entry);
}

public sealed class SystemSettingRepository(AppDbContext context) : ISystemSettingRepository
{
    public Task<SystemSetting?> GetAsync(string key, CancellationToken cancellationToken = default) =>
        context.SystemSettings.FirstOrDefaultAsync(s => s.Key == key, cancellationToken);

    public Task<List<SystemSetting>> ListAsync(CancellationToken cancellationToken = default) =>
        context.SystemSettings.OrderBy(s => s.Key).ToListAsync(cancellationToken);

    public void Add(SystemSetting setting) => context.SystemSettings.Add(setting);
}

internal static class SystemSettingExtensions
{
    public static async Task<int> GetIntAsync(this ISystemSettingRepository settings, string key, int fallback, CancellationToken cancellationToken)
    {
        var setting = await settings.GetAsync(key, cancellationToken);
        return int.TryParse(setting?.Value, out var value) ? value : fallback;
    }
}
