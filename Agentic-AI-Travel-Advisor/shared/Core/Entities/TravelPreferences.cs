namespace TravelAdvisor.Core.Entities;

public class TravelPreferences
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public decimal? BudgetMin { get; set; }
    public decimal? BudgetMax { get; set; }
    public string? PreferredClimate { get; set; }
    public string? Interests { get; set; }

    public ApplicationUser User { get; set; } = null!;
}
