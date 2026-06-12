using System.Security.Claims;
using MediAlert.Constants;
using MediAlert.DTOs.Caregiver;
using MediAlert.Services.Caregiver;
using MediAlert.Services.Caregiver.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediAlert.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class CaregiverController : ControllerBase
{
    private readonly ICaregiverService _caregiverService;

    public CaregiverController(ICaregiverService caregiverService)
    {
        _caregiverService = caregiverService;
    }

    [HttpPost("links/request")]
    [Authorize(Roles = UserRoles.Patient)]
    public async Task<IActionResult> RequestLink([FromBody] CreateCaregiverPatientLinkRequest request, CancellationToken cancellationToken)
    {
        var patientId = GetUserId();
        var result = await _caregiverService.SendLinkRequestAsync(patientId, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("links/{linkId}/approve")]
    [Authorize(Roles = UserRoles.Caregiver)]
    public async Task<IActionResult> ApproveLink(Guid linkId, CancellationToken cancellationToken)
    {
        var caregiverId = GetUserId();
        var result = await _caregiverService.ApproveLinkRequestAsync(linkId, caregiverId, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("links/{linkId}/reject")]
    [Authorize(Roles = UserRoles.Caregiver)]
    public async Task<IActionResult> RejectLink(Guid linkId, [FromBody] RejectCaregiverPatientLinkRequest request, CancellationToken cancellationToken)
    {
        var caregiverId = GetUserId();
        var result = await _caregiverService.RejectLinkRequestAsync(linkId, caregiverId, request.Reason, cancellationToken);
        return ToActionResult(result);
    }

    [HttpDelete("links/{linkId}/revoke")]
    [Authorize(Roles = UserRoles.Patient + "," + UserRoles.Caregiver)]
    public async Task<IActionResult> RevokeLink(Guid linkId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var result = await _caregiverService.RevokeLinkAsync(linkId, userId, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("links/caregiver")]
    [Authorize(Roles = UserRoles.Caregiver)]
    public async Task<IActionResult> GetCaregiverLinks(CancellationToken cancellationToken)
    {
        var caregiverId = GetUserId();
        var result = await _caregiverService.GetCaregiverLinksAsync(caregiverId, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("links/patient")]
    [Authorize(Roles = UserRoles.Patient)]
    public async Task<IActionResult> GetPatientLinks(CancellationToken cancellationToken)
    {
        var patientId = GetUserId();
        var result = await _caregiverService.GetPatientLinksAsync(patientId, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("patients/{patientId}/dashboard")]
    [Authorize(Roles = UserRoles.Caregiver)]
    public async Task<IActionResult> GetDashboard(Guid patientId, [FromQuery] int windowDays = 30, CancellationToken cancellationToken = default)
    {
        var caregiverId = GetUserId();
        var result = await _caregiverService.GetPatientDashboardAsync(caregiverId, patientId, windowDays, cancellationToken);
        return ToActionResult(result);
    }

    private Guid GetUserId() => MediAlert.Extensions.ClaimsPrincipalExtensions.GetUserId(User);

    private IActionResult ToActionResult<T>(CaregiverServiceResult<T> result)
    {
        if (result.Succeeded) return StatusCode(result.StatusCode, result.Data);
        
        return StatusCode(result.StatusCode, new CaregiverErrorResponse
        {
            Message = result.ErrorMessage ?? "Request failed.",
            ErrorCode = result.ErrorCode
        });
    }
}
