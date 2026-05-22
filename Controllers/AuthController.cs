using MediAlert.DTOs.Auth;
using MediAlert.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MediAlert.Controllers;

/// <summary>
/// HTTP API controller for authentication and user management.
///
/// ARCHITECTURE RULE:
/// Controllers contain ZERO business logic.
/// They only:
///   1. Receive the HTTP request
///   2. Validate model binding (DataAnnotations on DTOs)
///   3. Call the service
///   4. Map the service result to an HTTP response
///   5. Return the response
///
/// If you find yourself writing if/else business logic here, move it to AuthService.
///
/// BASE ROUTE: /api/auth
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST /api/auth/register
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Registers a new user account.
    ///
    /// Access: Public (no auth required)
    /// Success: 201 Created + AuthResponse (JWT token)
    /// Failure: 400/409/422 + AuthErrorResponse
    ///
    /// Maps to FR-01: User registers with email, password, and role.
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(AuthErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(AuthErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(AuthErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        // ModelState is automatically validated by [ApiController].
        // If DataAnnotations on RegisterRequest fail, ASP.NET Core returns
        // 400 Bad Request before this method even runs.
        // We don't need to check ModelState.IsValid manually.

        var result = await _authService.RegisterAsync(request);

        if (!result.Succeeded)
        {
            if (result.ValidationErrors is not null)
            {
                return UnprocessableEntity(new AuthErrorResponse
                {
                    Message = "Registration failed due to validation errors.",
                    Errors = result.ValidationErrors,
                });
            }

            return StatusCode(result.StatusCode, new AuthErrorResponse
            {
                Message = result.Error ?? "Registration failed.",
            });
        }

        // 201 Created — resource was successfully created.
        // The Location header should point to the new resource (user profile).
        // For now we return the auth response directly.
        return StatusCode(StatusCodes.Status201Created, result.Data);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST /api/auth/login
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Authenticates a user and returns a JWT token.
    ///
    /// Access: Public (no auth required)
    /// Success: 200 OK + AuthResponse (JWT token)
    /// Failure: 401 Unauthorized / 403 Forbidden + AuthErrorResponse
    ///
    /// Maps to FR-02: User logs in with email and password.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(AuthErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);

        if (!result.Succeeded)
        {
            return StatusCode(result.StatusCode, new AuthErrorResponse
            {
                Message = result.Error ?? "Login failed.",
            });
        }

        return Ok(result.Data);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET /api/auth/me
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the currently authenticated user's information from their JWT claims.
    ///
    /// Access: Any authenticated user
    /// Success: 200 OK + UserInfoResponse
    /// Failure: 401 Unauthorized
    ///
    /// This endpoint lets the Blazor frontend verify a stored token is still valid
    /// and retrieve current user info without re-logging in.
    ///
    /// NOTE: This reads from JWT claims, not the database.
    /// It's fast (no DB call) but may be slightly stale if the user was updated
    /// after the token was issued. For Phase 2, we'll add a refresh mechanism.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetCurrentUser()
    {
        // ClaimTypes.NameIdentifier = the "sub" claim = user's GUID
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email);
        var fullName = User.FindFirstValue(ClaimTypes.Name);
        var role = User.FindFirstValue(ClaimTypes.Role);
        var isVerified = User.FindFirstValue("IsEmailVerified");

        if (userId is null)
            return Unauthorized();

        return Ok(new
        {
            UserId = userId,
            Email = email,
            FullName = fullName,
            Role = role,
            IsEmailVerified = bool.TryParse(isVerified, out var v) && v,
        });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST /api/auth/users/{userId}/suspend
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Suspends a user account. Admin only.
    ///
    /// Access: [Authorize(Roles = "Admin")]
    /// Success: 200 OK
    /// Failure: 400/403/404
    ///
    /// Maps to FR-03: Admin suspends a user, blocking their login.
    /// </summary>
    [HttpPost("users/{userId}/suspend")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(AuthErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(AuthErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SuspendUser(string userId)
    {
        // Get the admin's ID from their JWT claims
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var result = await _authService.SuspendUserAsync(userId, adminId);

        if (!result.Succeeded)
        {
            return StatusCode(result.StatusCode, new AuthErrorResponse
            {
                Message = result.Error ?? "Operation failed.",
            });
        }

        return Ok(new { Message = "User has been suspended successfully." });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST /api/auth/users/{userId}/unsuspend
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Reinstates a suspended user account. Admin only.
    ///
    /// Maps to FR-03: Admin can reverse a suspension.
    /// </summary>
    [HttpPost("users/{userId}/unsuspend")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(AuthErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnsuspendUser(string userId)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _authService.UnsuspendUserAsync(userId, adminId);

        if (!result.Succeeded)
        {
            return StatusCode(result.StatusCode, new AuthErrorResponse
            {
                Message = result.Error ?? "Operation failed.",
            });
        }

        return Ok(new { Message = "User has been unsuspended successfully." });
    }
}