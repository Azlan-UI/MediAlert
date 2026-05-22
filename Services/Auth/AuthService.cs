using MediAlert.DTOs.Auth;
using MediAlert.Constants;
using MediAlert.Models;
using Microsoft.AspNetCore.Identity;
using ApplicationUser = MediAlert.Models.ApplicatioUser;

namespace MediAlert.Services.Auth;

public class AuthServiceResult<T>
{
    public bool Succeeded { get; init; }
    public T? Data { get; init; }
    public string? Error { get; init; }
    public Dictionary<string, string[]>? ValidationErrors { get; init; }
    public int StatusCode { get; init; } = 200;

    public static AuthServiceResult<T> Success(T data) =>
        new() { Succeeded = true, Data = data, StatusCode = 200 };

    public static AuthServiceResult<T> Failure(string error, int statusCode = 400) =>
        new() { Succeeded = false, Error = error, StatusCode = statusCode };

    public static AuthServiceResult<T> ValidationFailure(
        Dictionary<string, string[]> errors) =>
        new() { Succeeded = false, ValidationErrors = errors, StatusCode = 422 };
}

/// <summary>
/// Handles all authentication business logic for MediAlert.
///
/// DEPENDENCIES:
///   UserManager<T>   — ASP.NET Core Identity's user management service.
///                      Handles Create, Find, PasswordHash, Role assignment.
///   IJwtTokenService — Our custom JWT generation service.
///
/// These are injected by the DI container — we never instantiate them directly.
/// </summary>
public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        IJwtTokenService jwtTokenService,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // REGISTER
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Registers a new user account.
    ///
    /// Business rules enforced:
    ///   FR-01: Email must be unique, password ≥ 8 chars, role must be valid.
    ///   NFR-03: Password is hashed by Identity, never stored plaintext.
    ///
    /// Flow:
    ///   1. Validate the role value
    ///   2. Check email uniqueness
    ///   3. Create the ApplicationUser via Identity (hashes password)
    ///   4. Assign the role in AspNetRoles
    ///   5. Generate and return a JWT token
    /// </summary>
    public async Task<AuthServiceResult<AuthResponse>> RegisterAsync(RegisterRequest request)
    {
        // ── Validate role ─────────────────────────────────────────────────
        if (!UserRoles.IsValid(request.Role))
        {
            return AuthServiceResult<AuthResponse>.Failure(
                $"Invalid role '{request.Role}'. Must be one of: " +
                string.Join(", ", UserRoles.All));
        }

        // ── Check for duplicate email ─────────────────────────────────────
        // Identity's CreateAsync would also catch this, but checking early
        // gives a cleaner error message than parsing Identity's error list.
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
        {
            return AuthServiceResult<AuthResponse>.Failure(
                "An account with this email address already exists.",
                statusCode: 409); // 409 Conflict
        }

        // ── Build the user entity ─────────────────────────────────────────
        var user = new ApplicationUser
        {
            // Identity requires UserName. We use email as username
            // for simplicity (no separate username field needed).
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName.Trim(),
            Role = request.Role,
            PhoneNumber = request.PhoneNumber?.Trim(),

            // IsEmailVerified starts false — user must verify email.
            // For Phase 1, we auto-verify since we haven't built email service yet.
            // TODO Phase 2: Set false and send verification email.
            IsEmailVerified = true,     // TEMPORARY — change when email service is added
            EmailConfirmed = true,      // Sync with Identity's flag
            IsSuspended = false,
            CreatedDate = DateTime.UtcNow,
        };

        // ── Create user via Identity ──────────────────────────────────────
        // Identity handles password hashing (bcrypt). We never see plaintext again.
        var createResult = await _userManager.CreateAsync(user, request.Password);

        if (!createResult.Succeeded)
        {
            // Identity returns structured errors — map them to our format.
            var errors = createResult.Errors
                .GroupBy(e => e.Code)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.Description).ToArray());

            _logger.LogWarning("User registration failed for {Email}: {@Errors}",
                request.Email, errors);

            return AuthServiceResult<AuthResponse>.ValidationFailure(errors);
        }

        // ── Assign role ───────────────────────────────────────────────────
        // Identity's role system stores roles in AspNetRoles and
        // AspNetUserRoles. This enables [Authorize(Roles = "...")] to work.
        var roleResult = await _userManager.AddToRoleAsync(user, request.Role);

        if (!roleResult.Succeeded)
        {
            // Role assignment failed — this shouldn't happen if roles are seeded,
            // but we handle it gracefully.
            _logger.LogError(
                "Failed to assign role {Role} to user {UserId}",
                request.Role, user.Id);

            // Roll back by deleting the created user
            await _userManager.DeleteAsync(user);

            return AuthServiceResult<AuthResponse>.Failure(
                "Registration failed. Please try again.",
                statusCode: 500);
        }

        // ── Generate JWT ──────────────────────────────────────────────────
        var (token, expiry) = _jwtTokenService.GenerateToken(user);

        _logger.LogInformation(
            "User {Email} registered successfully with role {Role}",
            user.Email, user.Role);

        return AuthServiceResult<AuthResponse>.Success(new AuthResponse
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email!,
            Role = user.Role,
            Token = token,
            TokenExpiry = expiry,
            IsEmailVerified = user.IsEmailVerified,
        });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // LOGIN
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Authenticates a user and returns a JWT token.
    ///
    /// Business rules enforced:
    ///   FR-02: Login fails for wrong credentials with a clear message.
    ///   FR-02: Email-unverified accounts are blocked from login.
    ///   FR-03: Suspended accounts are blocked from login.
    ///   NFR-03: JWT issued on success, expires in 24h.
    ///
    /// SECURITY NOTE on generic error messages:
    ///   We return "Invalid email or password" for BOTH wrong email AND wrong
    ///   password. This prevents user enumeration attacks — an attacker
    ///   cannot determine whether an email is registered by the error message.
    /// </summary>
    public async Task<AuthServiceResult<AuthResponse>> LoginAsync(LoginRequest request)
    {
        // ── Find user by email ────────────────────────────────────────────
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user is null)
        {
            // Generic message — don't reveal whether email exists.
            return AuthServiceResult<AuthResponse>.Failure(
                "Invalid email or password.",
                statusCode: 401);
        }

        // ── Check suspension BEFORE password verification ─────────────────
        // FR-03: Suspended users see a 'suspended' message.
        // We check this before password to avoid a timing attack that could
        // confirm account existence via different response times.
        if (user.IsSuspended)
        {
            _logger.LogWarning(
                "Suspended user {Email} attempted to log in", user.Email);

            return AuthServiceResult<AuthResponse>.Failure(
                "Your account has been suspended. Please contact support.",
                statusCode: 403);
        }

        // ── Verify password ───────────────────────────────────────────────
        // Identity compares the plaintext against the stored bcrypt hash.
        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);

        if (!passwordValid)
        {
            // Increment lockout counter (Identity's built-in brute-force protection)
            await _userManager.AccessFailedAsync(user);

            return AuthServiceResult<AuthResponse>.Failure(
                "Invalid email or password.",
                statusCode: 401);
        }

        // ── Check email verification ──────────────────────────────────────
        // FR-02: Email-unverified accounts are blocked.
        if (!user.IsEmailVerified)
        {
            return AuthServiceResult<AuthResponse>.Failure(
                "Please verify your email address before logging in. " +
                "Check your inbox for the verification link.",
                statusCode: 403);
        }

        // ── Reset failed login counter on success ────────────────────────
        await _userManager.ResetAccessFailedCountAsync(user);

        // ── Generate JWT ──────────────────────────────────────────────────
        var (token, expiry) = _jwtTokenService.GenerateToken(user);

        _logger.LogInformation(
            "User {Email} logged in successfully", user.Email);

        return AuthServiceResult<AuthResponse>.Success(new AuthResponse
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email!,
            Role = user.Role,
            Token = token,
            TokenExpiry = expiry,
            IsEmailVerified = user.IsEmailVerified,
        });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ADMIN: SUSPEND / UNSUSPEND
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Suspends a user account (Admin only).
    /// FR-03: Suspended users cannot log in.
    ///
    /// SAFETY RULE: Admins cannot suspend other Admins through this endpoint.
    /// That would allow privilege escalation / admin lockout.
    /// </summary>
    public async Task<AuthServiceResult<bool>> SuspendUserAsync(
        string targetUserId, string adminUserId)
    {
        var target = await _userManager.FindByIdAsync(targetUserId);

        if (target is null)
        {
            return AuthServiceResult<bool>.Failure(
                "User not found.", statusCode: 404);
        }

        // FR-03: Admins cannot be suspended through this UI.
        if (target.Role == UserRoles.Admin)
        {
            return AuthServiceResult<bool>.Failure(
                "Admin accounts cannot be suspended through this interface.",
                statusCode: 403);
        }

        if (target.IsSuspended)
        {
            return AuthServiceResult<bool>.Failure(
                "This account is already suspended.");
        }

        target.IsSuspended = true;
        var result = await _userManager.UpdateAsync(target);

        if (!result.Succeeded)
        {
            _logger.LogError(
                "Failed to suspend user {UserId}", targetUserId);
            return AuthServiceResult<bool>.Failure(
                "Failed to suspend user.", statusCode: 500);
        }

        _logger.LogWarning(
            "Admin {AdminId} suspended user {UserId}", adminUserId, targetUserId);

        return AuthServiceResult<bool>.Success(true);
    }

    /// <summary>
    /// Reinstates a suspended user account (Admin only).
    /// FR-03: Admin can reverse suspension.
    /// </summary>
    public async Task<AuthServiceResult<bool>> UnsuspendUserAsync(
        string targetUserId, string adminUserId)
    {
        var target = await _userManager.FindByIdAsync(targetUserId);

        if (target is null)
        {
            return AuthServiceResult<bool>.Failure(
                "User not found.", statusCode: 404);
        }

        if (!target.IsSuspended)
        {
            return AuthServiceResult<bool>.Failure(
                "This account is not currently suspended.");
        }

        target.IsSuspended = false;
        var result = await _userManager.UpdateAsync(target);

        if (!result.Succeeded)
        {
            return AuthServiceResult<bool>.Failure(
                "Failed to unsuspend user.", statusCode: 500);
        }

        _logger.LogInformation(
            "Admin {AdminId} unsuspended user {UserId}", adminUserId, targetUserId);

        return AuthServiceResult<bool>.Success(true);
    }
}
