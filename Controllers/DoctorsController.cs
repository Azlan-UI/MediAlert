using System.Security.Claims;
using MediAlert.Constants;
using MediAlert.DTOs.Doctors;
using MediAlert.Services.Doctors;
using MediAlert.Services.Doctors.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediAlert.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class DoctorsController : ControllerBase
{
    private readonly IDoctorService _doctorService;

    public DoctorsController(IDoctorService doctorService)
    {
        _doctorService = doctorService;
    }

    [HttpGet("search")]
    [Authorize(Policy = AuthorizationPolicies.PremiumPatient)]
    public async Task<IActionResult> SearchDoctors([FromQuery] string? specialization, CancellationToken cancellationToken)
    {
        var result = await _doctorService.SearchDoctorsAsync(specialization, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("availability")]
    [Authorize(Roles = UserRoles.Doctor)]
    public async Task<IActionResult> AddAvailability([FromBody] AddAvailabilityRequest request, CancellationToken cancellationToken)
    {
        var doctorId = GetUserId();
        var result = await _doctorService.AddAvailabilityAsync(doctorId, request.DayOfWeek, request.StartTime, request.EndTime, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPut("{doctorId}/verify")]
    [Authorize(Roles = UserRoles.Admin)]
    public async Task<IActionResult> VerifyDoctor(Guid doctorId, CancellationToken cancellationToken)
    {
        var result = await _doctorService.VerifyDoctorAsync(doctorId, cancellationToken);
        return ToActionResult(result);
    }

    private Guid GetUserId() => MediAlert.Extensions.ClaimsPrincipalExtensions.GetUserId(User);

    private IActionResult ToActionResult<T>(DoctorServiceResult<T> result)
    {
        if (result.Succeeded) return StatusCode(result.StatusCode, result.Data);
        
        return StatusCode(result.StatusCode, new {
            Message = result.ErrorMessage ?? "Request failed.",
            ErrorCode = result.ErrorCode
        });
    }
}

public class AddAvailabilityRequest
{
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}
