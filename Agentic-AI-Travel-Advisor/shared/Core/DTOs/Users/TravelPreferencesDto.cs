namespace TravelAdvisor.Core.DTOs.Users;

public class TravelPreferencesDto
{
    public decimal? BudgetMin { get; set; }
    public decimal? BudgetMax { get; set; }
    public string? PreferredClimate { get; set; }
    public string? Interests { get; set; }

    /// <summary>budget, mid-range or luxury. Null leaves the saved value unchanged; empty clears it.</summary>
    public string? AccommodationPreference { get; set; }

    /// <summary>A transport mode name such as Train. Null leaves the saved value unchanged; empty clears it.</summary>
    public string? TransportPreference { get; set; }
}
