using Microsoft.AspNetCore.Identity;

namespace MediAlert.Models;

/// <summary>
/// Extends ASP.NET Core Identity's IdentityUser with MediAlert-specific fields.
///
/// ARCHITECTURE NOTE:
/// We inherit from IdentityUser because Identity already gives us:
///   - Id (GUID as string)
///   - Email + EmailConfirmed
///   - PasswordHash (bcrypt, never plaintext)
///   - UserName
///   - ConcurrencyStamp (optimistic concurrency)
///   - LockoutEnabled / LockoutEnd (brute-force protection)
/// We only add what Identity doesn't provide.
///
/// DATABASE NOTE:
/// EF Core will map this to the "AspNetUsers" table.
/// All extra properties become additional columns on that table.
/// </summary>
public class ApplicatioUser : IdentityUser
{
    /// <summary>
    /// The user's display name shown across the UI.
    /// Not nullable — every user must provide a name on registration.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// The user's role in the system.
    /// Must be one of: Patient, Caregiver, Doctor, Admin.
    ///
    /// WHY NOT JUST USE IDENTITY ROLES?
    /// Identity roles (AspNetRoles table) are designed for many-to-many
    /// user-role assignments (a user can be multiple roles). In MediAlert,
    /// each user has EXACTLY ONE role, so storing it as a discriminator
    /// column on the User table is simpler, faster, and matches the ERD.
    ///
    /// We still wire up Identity roles for [Authorize(Roles = "...")] to work.
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Whether the user has verified their email address.
    ///
    /// WHY NOT USE Identity's EmailConfirmed?
    /// We are — this maps to Identity's EmailConfirmed under the hood.
    /// We expose it under our domain name for clarity in business logic.
    /// In the DB context we sync this with EmailConfirmed.
    ///
    /// Business rule (FR-02): Unverified accounts cannot log in.
    /// </summary>
    public bool IsEmailVerified { get; set; } = false;

    /// <summary>
    /// Administrative suspension flag.
    /// When true, the user cannot log in even with correct credentials.
    ///
    /// Business rule (FR-03): Suspended users see a 'suspended' message.
    /// This is separate from Identity's LockoutEnd (which is time-based).
    /// Our suspension is indefinite until an Admin reverses it.
    /// </summary>
    public bool IsSuspended { get; set; } = false;

    /// <summary>
    /// UTC timestamp of account creation.
    /// Set once on registration, never changed.
    /// Required for audit trails in a healthcare application.
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}


