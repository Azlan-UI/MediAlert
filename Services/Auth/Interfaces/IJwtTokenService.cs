using MediAlert.Configuration;
using MediAlert.Models;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text;
using ApplicationUser = MediAlert.Models.ApplicatioUser;

namespace MediAlert.Services.Auth;

/// <summary>
/// Contract for JWT token generation.
/// Using an interface allows us to mock this in unit tests.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generates a signed JWT token for the given user.
    /// </summary>
    /// <param name="user">The authenticated ApplicationUser.</param>
    /// <returns>The token string and its expiry timestamp.</returns>
    (string Token, DateTime Expiry) GenerateToken(ApplicationUser user);
}

/// <summary>
/// Generates signed JWT tokens for authenticated MediAlert users.
/// </summary>
