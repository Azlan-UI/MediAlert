using MediAlert.Constants;
using MediAlert.DTOs.Admin;
using MediAlert.Services.Admin.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediAlert.Controllers;

[ApiController]
[Authorize(Roles = UserRoles.Admin)]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
    {
        var users = await _adminService.GetUsersAsync(cancellationToken);
        return Ok(users);
    }

    [HttpPut("users/{userId}/toggle-suspension")]
    public async Task<IActionResult> ToggleUserSuspension(string userId, CancellationToken cancellationToken)
    {
        var success = await _adminService.ToggleUserSuspensionAsync(userId, cancellationToken);
        if (!success)
            return BadRequest(new { Message = "Cannot suspend user (may be an Admin or not found)." });

        return Ok(new { Message = "Suspension status toggled." });
    }

    [HttpDelete("users/{userId}")]
    public async Task<IActionResult> DeleteUser(string userId, CancellationToken cancellationToken)
    {
        var error = await _adminService.DeleteUserAsync(userId, cancellationToken);
        if (error != null)
            return BadRequest(new { Message = error });

        return NoContent();
    }

    [HttpGet("doctors")]
    public async Task<IActionResult> GetDoctors(CancellationToken cancellationToken)
    {
        var doctors = await _adminService.GetDoctorsAsync(cancellationToken);
        return Ok(doctors);
    }

    [HttpPut("doctors/{doctorId}/verify")]
    public async Task<IActionResult> VerifyDoctor(Guid doctorId, [FromBody] VerifyDoctorRequest request, CancellationToken cancellationToken)
    {
        var success = await _adminService.VerifyDoctorAsync(doctorId, request.VerificationStatus, cancellationToken);
        if (!success)
            return BadRequest(new { Message = "Failed to update doctor verification status." });

        return Ok(new { Message = "Doctor status updated." });
    }

    [HttpGet("consultations")]
    public async Task<IActionResult> GetConsultations(CancellationToken cancellationToken)
    {
        var consultations = await _adminService.GetConsultationsAsync(cancellationToken);
        return Ok(consultations);
    }

    [HttpPut("consultations/{consultationId:guid}/cancel")]
    public async Task<IActionResult> CancelConsultation(Guid consultationId, CancellationToken cancellationToken)
    {
        try
        {
            var success = await _adminService.CancelConsultationAsync(consultationId, cancellationToken);
            if (!success)
                return BadRequest(new { Message = "Cannot cancel consultation (may be completed or already cancelled)." });

            return Ok(new { Message = "Consultation cancelled successfully." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpGet("subscriptions")]
    public async Task<IActionResult> GetSubscriptions(CancellationToken cancellationToken)
    {
        var subscriptions = await _adminService.GetSubscriptionsAsync(cancellationToken);
        return Ok(subscriptions);
    }
}
