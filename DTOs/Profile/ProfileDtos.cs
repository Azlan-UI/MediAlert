using System.ComponentModel.DataAnnotations;

namespace MediAlert.DTOs.Profile;

public class UserProfileDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Role { get; set; } = string.Empty;
}

public class UpdateProfileRequest
{
    [Required]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Phone]
    [RegularExpression(@"^\+?[1-9]\d{1,14}$", ErrorMessage = "Invalid phone number format. Must be a valid E.164 phone number.")]
    public string? PhoneNumber { get; set; }
}
