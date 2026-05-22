using MediAlert.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ApplicationUser = MediAlert.Models.ApplicatioUser;

namespace MediAlert.Services.Auth;

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _jwtSettings;

    /// <summary>
    /// IOptions<JwtSettings> is injected by the DI container.
    /// The settings come from appsettings.json → "JwtSettings" section.
    /// </summary>
    public JwtTokenService(IOptions<JwtSettings> jwtOptions)
    {
        _jwtSettings = jwtOptions.Value;
    }

    /// <inheritdoc/>
    public (string Token, DateTime Expiry) GenerateToken(ApplicationUser user)
    {
        // ── Step 1: Define the signing credentials ────────────────────────
        // The secret key must be at least 256 bits for HMAC-SHA256.
        // We encode the string to bytes, then create a SymmetricSecurityKey.
        var keyBytes = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);
        var signingKey = new SymmetricSecurityKey(keyBytes);
        var credentials = new SigningCredentials(
            signingKey,
            SecurityAlgorithms.HmacSha256);

        // ── Step 2: Define the claims (the token's payload) ───────────────
        // Claims are key-value pairs embedded in the token.
        // ASP.NET Core automatically maps these to User.Identity and
        // User.Claims inside controllers and Blazor pages.
        var claims = new List<Claim>
        {
            // Subject — the user's unique ID. Standard JWT claim.
            // Available as: User.FindFirstValue(ClaimTypes.NameIdentifier)
            new(JwtRegisteredClaimNames.Sub, user.Id),
 
            // Email — for display and lookups without a DB call.
            // Available as: User.FindFirstValue(ClaimTypes.Email)
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
 
            // JWT ID — unique identifier for this token.
            // Useful for token revocation systems (future enhancement).
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
 
            // Full name — for display in the Blazor UI without a DB call.
            new(ClaimTypes.Name, user.FullName),
 
            // Role — THIS IS CRITICAL.
            // [Authorize(Roles = "Admin")] reads this claim.
            // Without this, role-based authorization won't work.
            new(ClaimTypes.Role, user.Role),
 
            // Custom MediAlert claims — useful in Blazor components
            // to check suspension status without a DB query.
            new("IsEmailVerified", user.IsEmailVerified.ToString()),
            new("IsSuspended", user.IsSuspended.ToString()),
        };

        // ── Step 3: Calculate expiry ──────────────────────────────────────
        // NFR-03: JWT TTL ≤ 24 hours.
        var expiry = DateTime.UtcNow.AddHours(_jwtSettings.ExpiryHours);

        // ── Step 4: Build the token ───────────────────────────────────────
        var tokenDescriptor = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,  // Token not valid before now
            expires: expiry,
            signingCredentials: credentials);

        // ── Step 5: Serialize to string ───────────────────────────────────
        var tokenString = new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);

        return (tokenString, expiry);
    }
}
