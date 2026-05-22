
using Microsoft.AspNetCore.Identity;

namespace MediAlert.Constants;

public static class UserRoles
{
    public const string Patient = "Patient";
    public const string Caregiver = "Caregiver";
    public const string Doctor = "Doctor";
    public const string Admin = "Admin";

    /// <summary>
    /// All valid role values — used for validation.
    /// </summary>
    public static readonly IReadOnlyList<string> All =
        new[] { Patient, Caregiver, Doctor, Admin };

    /// <summary>
    /// Returns true if the given role string is a valid MediAlert role.
    /// </summary>
    public static bool IsValid(string role) =>
        All.Contains(role, StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Role constants used throughout the application.
///
/// WHY A STATIC CLASS?
/// Magic strings ("Patient", "Admin") scattered in controllers and services
/// are a maintenance nightmare. One typo and auth breaks silently.
/// Using constants means the compiler catches typos.
///
/// USAGE:
///   [Authorize(Roles = UserRoles.Admin)]
///   if (user.Role == UserRoles.Patient) { ... }
/// </summary>
