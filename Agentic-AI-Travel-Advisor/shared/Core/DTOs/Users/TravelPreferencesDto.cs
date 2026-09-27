namespace TravelAdvisor.Core.DTOs.Users;

public class TravelPreferencesDto
{
    public decimal? BudgetMin { get; set; }
    public decimal? BudgetMax { get; set; }
    public string? PreferredClimate { get; set; }
    public string? Interests { get; set; }
}
