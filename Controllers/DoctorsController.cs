using System.Security.Claims;
using MediAlert.Constants;
using MediAlert.DTOs.Doctors;
using MediAlert.Services.Doctors;
using MediAlert.Services.Doctors.Interfaces;
using MediAlert.Services.Billing.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediAlert.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class DoctorsController : ControllerBase
{
    private readonly IDoctorService _doctorService;
    private readonly IStripeBillingService _billingService;

    public DoctorsController(IDoctorService doctorService, IStripeBillingService billingService)
    {
        _doctorService = doctorService;
        _billingService = billingService;
    }

    [HttpGet("search")]
    [Authorize(Roles = UserRoles.Patient)]
    public async Task<IActionResult> SearchDoctors([FromQuery] string? specialization, CancellationToken cancellationToken)
    {
        var result = await _doctorService.SearchDoctorsAsync(specialization, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("{doctorId}")]
    [Authorize]
    public async Task<IActionResult> GetDoctorProfile(Guid doctorId, CancellationToken cancellationToken)
    {
        var result = await _doctorService.GetDoctorProfileAsync(doctorId, cancellationToken);
        return ToActionResult(result);
    }


    [HttpGet("dashboard")]
    [Authorize(Roles = UserRoles.Doctor)]
    public async Task<IActionResult> GetDashboardData(CancellationToken cancellationToken)
    {
        var doctorId = GetUserId();
        var hasAccess = await _billingService.HasDoctorPremiumAccessAsync(doctorId, cancellationToken);
        if (!hasAccess)
        {
            return StatusCode(StatusCodes.Status402PaymentRequired, new { Message = "Doctor subscription required to access the dashboard." });
        }

        var result = await _doctorService.GetDashboardDataAsync(doctorId.ToString(), cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("subscription/checkout")]
    [Authorize(Roles = UserRoles.Doctor)]
    public async Task<IActionResult> CreateDoctorCheckout([FromBody] MediAlert.DTOs.Billing.CreateSubscriptionCheckoutRequest request, CancellationToken cancellationToken)
    {
        var doctorId = GetUserId();
        var result = await _billingService.CreateDoctorCheckoutSessionAsync(doctorId, request, cancellationToken);
        if (result.Succeeded) return StatusCode(result.StatusCode, result.Data);
        
        return StatusCode(result.StatusCode, new {
            Message = result.ErrorMessage ?? "Request failed.",
            ErrorCode = result.ErrorCode
        });
    }

    [HttpPost("subscription/verify")]
    [Authorize(Roles = UserRoles.Doctor)]
    public async Task<IActionResult> VerifyDoctorCheckout([FromQuery] string sessionId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sessionId)) return BadRequest();
        var doctorId = GetUserId();
        var result = await _billingService.VerifyDoctorCheckoutSessionAsync(doctorId, sessionId, cancellationToken);
        
        if (result.Succeeded) return StatusCode(result.StatusCode, result.Data);
        return StatusCode(result.StatusCode, new {
            Message = result.ErrorMessage ?? "Request failed.",
            ErrorCode = result.ErrorCode
        });
    }

    [HttpGet("subscription/premium-access")]
    [Authorize(Roles = UserRoles.Doctor)]
    public async Task<IActionResult> HasDoctorPremiumAccess(CancellationToken cancellationToken)
    {
        var doctorId = GetUserId();
        var hasAccess = await _billingService.HasDoctorPremiumAccessAsync(doctorId, cancellationToken);
        return Ok(new { HasPremiumAccess = hasAccess });
    }

    [HttpPost("availability")]
    [Authorize(Roles = UserRoles.Doctor)]
    public async Task<IActionResult> AddAvailability([FromBody] CreateDoctorAvailabilityRequest request, CancellationToken cancellationToken)
    {
        var doctorId = GetUserId();
        var result = await _doctorService.AddAvailabilityAsync(doctorId, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("availability")]
    [Authorize(Roles = UserRoles.Doctor)]
    public async Task<IActionResult> GetAvailabilities(CancellationToken cancellationToken)
    {
        var doctorId = GetUserId();
        var result = await _doctorService.GetAvailabilitiesAsync(doctorId, cancellationToken);
        return ToActionResult(result);
    }

    [HttpDelete("availability/{availabilityId}")]
    [Authorize(Roles = UserRoles.Doctor)]
    public async Task<IActionResult> RemoveAvailability(Guid availabilityId, CancellationToken cancellationToken)
    {
        var doctorId = GetUserId();
        var result = await _doctorService.RemoveAvailabilityAsync(doctorId, availabilityId, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("consultations/{consultationId}")]
    [Authorize]
    public async Task<IActionResult> GetConsultationDetails(Guid consultationId, CancellationToken cancellationToken)
    {
        var userId = GetUserId().ToString();
        var result = await _doctorService.GetConsultationDetailsAsync(consultationId, userId, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("{doctorId}/availability")]
    [Authorize(Roles = UserRoles.Patient)]
    public async Task<IActionResult> GetDoctorAvailabilities(Guid doctorId, CancellationToken cancellationToken)
    {
        var result = await _doctorService.GetAvailabilitiesAsync(doctorId, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("{doctorId}/booked-slots")]
    [Authorize(Roles = UserRoles.Patient)]
    public async Task<IActionResult> GetBookedSlots(Guid doctorId, [FromQuery] DateTime date, CancellationToken cancellationToken)
    {
        var result = await _doctorService.GetBookedSlotsAsync(doctorId, date, cancellationToken);
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


