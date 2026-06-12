using MediAlert.DTOs.Auth;

namespace MediAlert.Services.Auth;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task RegisterAsync(RegisterRequest request);
    Task<string> GeneratePasswordResetTokenAsync(string email);
    Task ResetPasswordAsync(string email, string token, string newPassword);
}
