namespace TravelAdvisor.Core.Entities;

public class TravelPreferences : IAuditable
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public decimal? BudgetMin { get; set; }
    public decimal? BudgetMax { get; set; }
    public string? PreferredClimate { get; set; }
    public string? Interests { get; set; }

    /// <summary>budget, mid-range or luxury.</summary>
    public string? AccommodationPreference { get; set; }

    /// <summary>A <see cref="Enums.TransportMode"/> name, e.g. Train.</summary>
    public string? TransportPreference { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser User { get; set; } = null!;
}
