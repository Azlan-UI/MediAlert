using System.ComponentModel.DataAnnotations;
using MediAlert.Models;

namespace MediAlert.DTOs.Auth;
/// Data sent by the client when logging in.
/// POST /api/auth/login
///
/// Maps to FR-02: Patient logs in with email and password.
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// The registered email address.
    /// </summary>
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Please provide a valid email address.")]
    [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        ErrorMessage = "Email must be in the format name@example.com.")]
    [StringLength(256, ErrorMessage = "Email must not exceed 256 characters.")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// The plaintext password. Compared against the stored hash.
    /// </summary>
    [Required(ErrorMessage = "Password is required.")]
    public string Password { get; set; } = string.Empty;
}
