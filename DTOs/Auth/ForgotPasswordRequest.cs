using System.ComponentModel.DataAnnotations;

namespace MediAlert.DTOs.Auth;

public sealed class ForgotPasswordRequest
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string Email { get; set; } = string.Empty;
}
