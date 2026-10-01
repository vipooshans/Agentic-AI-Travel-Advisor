namespace TravelAdvisor.Core.Entities;

/// <summary>Per-night override for a room: block the date or charge a different price.</summary>
public class RoomAvailability : IAuditable
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public DateOnly Date { get; set; }
    public bool IsBlocked { get; set; }
    public decimal? PriceOverride { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Room Room { get; set; } = null!;
}
