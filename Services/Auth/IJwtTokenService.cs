using MediAlert.Models;

namespace MediAlert.Services.Auth;

public interface IJwtTokenService
{
    string GenerateToken(ApplicatioUser user);
}
