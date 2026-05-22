using System.ComponentModel.DataAnnotations;
using MediAlert.Models;

namespace MediAlert.DTOs.Auth;

/// Returned to the client after successful registration or login.
///
/// SECURITY RULE: This DTO must NEVER contain PasswordHash or any
/// sensitive Identity internals.
///
/// The Token field is a signed JWT — the client stores this and sends it
/// as "Authorization: Bearer {token}" on every subsequent request.
/// </summary>
public class AuthResponse
{
    /// <summary>
    /// The user's GUID identifier.
    /// Clients use this to identify themselves in API calls.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// The user's full name for UI display.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// The user's email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// The user's assigned role.
    /// Used by the Blazor frontend to show role-appropriate navigation.
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// The signed JWT token.
    /// Client must store this (localStorage or cookie) and send it on every request.
    /// Expires after 24 hours (NFR-03).
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// When the token expires (UTC).
    /// Helps the client know when to request a new token.
    /// </summary>
    public DateTime TokenExpiry { get; set; }

    /// <summary>
    /// Whether the email has been verified.
    /// The frontend uses this to show an "Unverified — please check your email" banner.
    /// </summary>
    public bool IsEmailVerified { get; set; }
}
