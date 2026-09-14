namespace TravelAdvisor.Core.Entities;

public class Room
{
    public int Id { get; set; }
    public int HotelId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RoomType { get; set; } = string.Empty;
    public decimal PricePerNight { get; set; }
    public int Capacity { get; set; }
    public bool IsAvailable { get; set; } = true;

    public Hotel Hotel { get; set; } = null!;
    public ICollection<Booking> Bookings { get; set; } = [];
}
