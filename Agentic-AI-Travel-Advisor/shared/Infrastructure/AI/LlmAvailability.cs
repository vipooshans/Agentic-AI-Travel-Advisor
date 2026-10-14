namespace TravelAdvisor.Infrastructure.AI;

/// <summary>
/// Remembers that the model provider rejected the API key, so requests go straight to the
/// deterministic planner instead of paying for a failed round trip every time.
/// </summary>
public sealed class LlmAvailability(TimeProvider clock)
{
    public static readonly TimeSpan CoolDown = TimeSpan.FromMinutes(10);

    private long _suspendedUntilTicks;

    public bool IsSuspended => clock.GetUtcNow().UtcTicks < Interlocked.Read(ref _suspendedUntilTicks);

    public DateTimeOffset SuspendedUntil => new(Interlocked.Read(ref _suspendedUntilTicks), TimeSpan.Zero);

    public void Suspend() => Interlocked.Exchange(ref _suspendedUntilTicks, (clock.GetUtcNow() + CoolDown).UtcTicks);
}
