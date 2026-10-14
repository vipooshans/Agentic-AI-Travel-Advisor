namespace TravelAdvisor.Core.DTOs.Users;

public class UserProfileDto
{
    public string? PhoneNumber { get; set; }
    public string? Nationality { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public string PreferredCurrency { get; set; } = "LKR";
}
