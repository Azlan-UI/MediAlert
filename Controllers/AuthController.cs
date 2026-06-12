using MediAlert.DTOs.Auth;
using MediAlert.Services.Auth;
using MediAlert.Services.Notifications;
using Microsoft.AspNetCore.Mvc;

namespace MediAlert.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IEmailService _emailService;

    public AuthController(IAuthService authService, IEmailService emailService)
    {
        _authService = authService;
        _emailService = emailService;
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

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var token = await _authService.GeneratePasswordResetTokenAsync(request.Email);
        
        if (!string.IsNullOrEmpty(token))
        {
            var scheme = Request.Scheme;
            var host = Request.Host;
            var resetLink = $"{scheme}://{host}/reset-password?email={Uri.EscapeDataString(request.Email)}&token={Uri.EscapeDataString(token)}";

            var subject = "MediAlert Password Reset Request";
            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e5e7eb; border-radius: 8px;'>
                    <h2 style='color: #4338ca; text-align: center;'>MediAlert</h2>
                    <h3 style='color: #1f2937;'>Password Reset Request</h3>
                    <p style='color: #4b5563; line-height: 1.5;'>We received a request to reset your password for your MediAlert account.</p>
                    <p style='color: #4b5563; line-height: 1.5;'>Please click the button below to reset your password:</p>
                    <div style='text-align: center; margin: 25px 0;'>
                        <a href='{resetLink}' style='background-color: #4338ca; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: bold; display: inline-block;'>Reset Password</a>
                    </div>
                    <p style='color: #9ca3af; font-size: 0.85rem; line-height: 1.5; margin-top: 30px; border-top: 1px solid #f3f4f6; padding-top: 15px;'>
                        If you did not request this, you can safely ignore this email. This link will expire shortly.
                    </p>
                </div>";

            await _emailService.SendEmailAsync(request.Email, subject, body);
        }

        return Ok(new { Message = "If that email is registered, a password reset link has been sent.", Token = token });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            await _authService.ResetPasswordAsync(request.Email, request.Token, request.NewPassword);
            return Ok(new { Message = "Password has been successfully reset." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new AuthErrorResponse { Message = ex.Message });
        }
    }
}
