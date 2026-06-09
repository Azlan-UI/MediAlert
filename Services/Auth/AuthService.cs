using MediAlert.Data;
using MediAlert.DTOs.Auth;
using MediAlert.Models;
using Microsoft.AspNetCore.Identity;

namespace MediAlert.Services.Auth;

public sealed class AuthService : IAuthService
{
    private readonly UserManager<ApplicatioUser> _userManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ApplicationDbContext _db;

    public AuthService(
        UserManager<ApplicatioUser> userManager,
        IJwtTokenService jwtTokenService,
        ApplicationDbContext db)
    {
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
        _db = db;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
            throw new UnauthorizedAccessException("Invalid email or password.");

        if (user.IsSuspended)
            throw new UnauthorizedAccessException("This account has been suspended.");

        if (!user.IsEmailVerified)
            throw new UnauthorizedAccessException("Email not verified. Please check your inbox.");

        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
            throw new UnauthorizedAccessException("Invalid email or password.");

        var token = _jwtTokenService.GenerateToken(user);

        return new AuthResponse
        {
            Token = token,
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Role = user.Role,
        };
    }

    public async Task RegisterAsync(RegisterRequest request)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
            throw new UnauthorizedAccessException("An account with this email already exists.");

        var user = new ApplicatioUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            Role = request.Role,
            IsEmailVerified = true, // Auto-verify for seamless login
            CreatedDate = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Registration failed: {errors}");
        }

        // Create the corresponding role-specific record
        if (request.Role == Constants.UserRoles.Patient)
        {
            _db.Patients.Add(new MediAlert.Models.Patient { UserId = user.Id });
        }
        else if (request.Role == Constants.UserRoles.Caregiver)
        {
            _db.Caregivers.Add(new MediAlert.Models.Caregiver { UserId = user.Id });
        }
        else if (request.Role == Constants.UserRoles.Doctor)
        {
            _db.Doctors.Add(new MediAlert.Models.Doctor { UserId = user.Id, Specialization = "General", Qualifications = "N/A" });
        }
        await _db.SaveChangesAsync();

        // Add user to their role if roles are managed by Identity
        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            try
            {
                await _userManager.AddToRoleAsync(user, request.Role);
            }
            catch
            {
                // If role doesn't exist yet, ignore or handle appropriately
            }
        }
    }
}
