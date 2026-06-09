using MediAlert.DTOs.Auth;
using MediAlert.Services.Auth;
using Microsoft.AspNetCore.Mvc;

namespace MediAlert.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var response = await _authService.LoginAsync(request);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new AuthErrorResponse { Message = ex.Message });
        }
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            await _authService.RegisterAsync(request);
            return Ok(new { Message = "Registration successful" });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Conflict(new AuthErrorResponse { Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new AuthErrorResponse { Message = ex.Message });
        }
    }
}
