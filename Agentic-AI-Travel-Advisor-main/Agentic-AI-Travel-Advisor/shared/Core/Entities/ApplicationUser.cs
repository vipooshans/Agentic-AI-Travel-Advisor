using Microsoft.AspNetCore.Identity;

namespace TravelAdvisor.Core.Entities;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Role Role { get; set; } = null!;
    public TravelPreferences? TravelPreferences { get; set; }
    public ICollection<Hotel> OwnedHotels { get; set; } = [];
    public ICollection<TravelPackage> TravelPackages { get; set; } = [];
    public ICollection<Booking> Bookings { get; set; } = [];
    public ICollection<Itinerary> Itineraries { get; set; } = [];
    public ICollection<AIConversation> AIConversations { get; set; } = [];
}
