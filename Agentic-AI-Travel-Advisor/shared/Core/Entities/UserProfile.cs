namespace TravelAdvisor.Core.Entities;

public class UserProfile : IAuditable
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Nationality { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public string PreferredCurrency { get; set; } = "LKR";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser User { get; set; } = null!;
}
