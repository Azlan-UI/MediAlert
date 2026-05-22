using System.ComponentModel.DataAnnotations;
using MediAlert.Models;

namespace MediAlert.DTOs.Auth;
/// Standard error response structure for authentication failures.
/// Ensures the API always returns structured JSON errors, not HTML error pages.
/// </summary>
public class AuthErrorResponse
{
    /// <summary>
    /// Human-readable error message safe to show to users.
    /// Never expose internal details (stack traces, SQL errors).
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Optional list of field-level validation errors.
    /// Key = field name, Value = error message list.
    /// Used by the frontend to highlight specific form fields.
    /// </summary>
    public Dictionary<string, string[]>? Errors { get; set; }
}