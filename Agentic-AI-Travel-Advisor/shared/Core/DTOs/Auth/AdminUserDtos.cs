using System.ComponentModel.DataAnnotations;

namespace TravelAdvisor.Core.DTOs.Auth;

public class UpdateProfileRequest
{
    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;
}

public class CreateStaffUserRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = string.Empty;
}

public class UpdateUserActiveRequest
{
    public bool IsActive { get; set; }
}
