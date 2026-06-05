using MediAlert.DTOs.Auth;

namespace MediAlert.Services.Auth;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request);
}
