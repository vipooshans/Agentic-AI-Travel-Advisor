namespace TravelAdvisor.Core.Entities;

public class Room : IAuditable
{
    public int Id { get; set; }
    public int HotelId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RoomType { get; set; } = string.Empty;
    public decimal PricePerNight { get; set; }
    public int Capacity { get; set; }

    /// <summary>Master switch set by the owner. Per-date blocks and price overrides live in <see cref="RoomAvailability"/>.</summary>
    public bool IsAvailable { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Hotel Hotel { get; set; } = null!;
    public ICollection<Booking> Bookings { get; set; } = [];
    public ICollection<RoomAvailability> Availability { get; set; } = [];
}
