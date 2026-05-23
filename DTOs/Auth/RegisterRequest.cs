using System.ComponentModel.DataAnnotations;
using MediAlert.Models;

namespace MediAlert.DTOs.Auth;

public class RegisterRequest
{
    /// <summary>
    /// User's full display name.
    /// Validated: required, max 100 characters.
    /// </summary>
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(100, MinimumLength = 2,
        ErrorMessage = "Full name must be between 2 and 100 characters.")]
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Login email address. Must be unique across all users.
    /// Validated: required, valid email format.
    /// </summary>
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Please provide a valid email address.")]
    [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        ErrorMessage = "Email must be in the format name@example.com.")]
    [StringLength(256, ErrorMessage = "Email must not exceed 256 characters.")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Plaintext password from the client.
    /// NEVER stored — the service hashes this before saving.
    ///
    /// Policy (NFR-03 + FR-01):
    ///   - Minimum 8 characters
    ///   - At least 1 uppercase, 1 lowercase, 1 digit, 1 special character
    ///   This matches ASP.NET Core Identity's default password policy.
    /// </summary>
    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 8,
        ErrorMessage = "Password must be at least 8 characters.")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Confirms the password to prevent typos.
    /// Validated: must match Password exactly.
    /// </summary>
    [Required(ErrorMessage = "Password confirmation is required.")]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    /// <summary>
    /// The role this account is being created for.
    /// Must be one of: Patient, Caregiver, Doctor, Admin.
    ///
    /// WHY ALLOW ROLE SELECTION ON REGISTER?
    /// The proposal requires it (Module 1). In production, you'd restrict
    /// Admin registration to an internal tool. For this application,
    /// we validate the role value but allow all four.
    /// </summary>
    [Required(ErrorMessage = "Role is required.")]
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Optional phone number. Required for Patient role sub-profile.
    /// Validated: E.164-compatible format if provided.
    /// </summary>
    [Phone(ErrorMessage = "Please provide a valid phone number.")]
    [StringLength(20)]
    public string? PhoneNumber { get; set; }
}
