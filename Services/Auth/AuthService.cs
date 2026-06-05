using MediAlert.DTOs.Auth;
using MediAlert.Models;
using Microsoft.AspNetCore.Identity;

namespace MediAlert.Services.Auth;

public sealed class AuthService : IAuthService
{
    private readonly UserManager<ApplicatioUser> _userManager;
    private readonly SignInManager<ApplicatioUser> _signInManager;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthService(
        UserManager<ApplicatioUser> userManager,
        SignInManager<ApplicatioUser> signInManager,
        IJwtTokenService jwtTokenService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenService = jwtTokenService;
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

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
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
}
