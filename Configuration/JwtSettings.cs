namespace MediAlert.Configuration;

/// <summary>
/// Strongly-typed configuration class for JWT settings.
///
/// WHY STRONGLY-TYPED CONFIGURATION?
/// Instead of: Configuration["Jwt:SecretKey"]  ← string keys, no IntelliSense, runtime errors
/// We use: JwtSettings.SecretKey               ← type-safe, compile-time errors
///
/// This class maps to the "JwtSettings" section in appsettings.json.
/// Program.cs registers it via: builder.Services.Configure<JwtSettings>(...)
/// Services inject it via: IOptions<JwtSettings>
/// </summary>
public class JwtSettings
{
    /// <summary>
    /// The secret key used to sign JWT tokens.
    ///
    /// SECURITY REQUIREMENTS:
    ///   - Minimum 256 bits (32 characters) for HMAC-SHA256
    ///   - NEVER commit this value to Git
    ///   - In production: use environment variables or Azure Key Vault
    ///   - For development: use User Secrets (right-click project → Manage User Secrets)
    ///
    /// If this key leaks, attackers can forge tokens for ANY user including Admin.
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// The token issuer — who created this token.
    /// Must match in both generation and validation.
    /// Typically your app's domain or name.
    /// Example: "MediAlert"
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// The token audience — who this token is intended for.
    /// Must match in both generation and validation.
    /// Example: "MediAlertUsers"
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// How long (in hours) before the token expires.
    ///
    /// NFR-03 requirement: JWT TTL ≤ 24 hours.
    /// We use 24 hours. After expiry, the user must log in again.
    ///
    /// TRADEOFF: Shorter = more secure (less exposure if token stolen),
    ///           Longer = better UX (user not constantly re-logging in).
    /// 24h is a good balance for a healthcare app that's not a banking system.
    /// </summary>
    public int ExpiryHours { get; set; } = 24;
}