using System.Security.Claims;
using MediAlert.Constants;
using MediAlert.DTOs.Appointments;
using MediAlert.Services.Appointments;
using MediAlert.Services.Appointments.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediAlert.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;

    public AppointmentsController(IAppointmentService appointmentService)
    {
        _appointmentService = appointmentService;
    }

    [HttpGet]
    [Authorize(Roles = UserRoles.Patient)]
    public async Task<IActionResult> GetMyAppointments(CancellationToken cancellationToken)
    {
        var patientId = GetUserId();
        var result = await _appointmentService.GetPatientAppointmentsAsync(patientId, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost]
    [Authorize(Roles = UserRoles.Patient)]
    public async Task<IActionResult> BookAppointment([FromBody] CreateAppointmentRequest request, CancellationToken cancellationToken)
    {
        var patientId = GetUserId();
        var result = await _appointmentService.BookAppointmentAsync(patientId, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPut("{appointmentId}/reschedule")]
    [Authorize(Roles = UserRoles.Patient)]
    public async Task<IActionResult> RescheduleAppointment(Guid appointmentId, [FromBody] DateTime newTime, CancellationToken cancellationToken)
    {
        var patientId = GetUserId();
        var result = await _appointmentService.RescheduleAppointmentAsync(appointmentId, patientId, newTime, cancellationToken);
        return ToActionResult(result);
    }

    [HttpDelete("{appointmentId}")]
    [Authorize(Roles = UserRoles.Patient)]
    public async Task<IActionResult> CancelAppointment(Guid appointmentId, CancellationToken cancellationToken)
    {
        var patientId = GetUserId();
        var result = await _appointmentService.CancelAppointmentAsync(appointmentId, patientId, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("refills/generate")]
    [Authorize(Roles = UserRoles.Patient)]
    public async Task<IActionResult> GenerateRefills(CancellationToken cancellationToken)
    {
        var patientId = GetUserId();
        var result = await _appointmentService.GenerateRefillRemindersAsync(patientId, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("refills")]
    [Authorize(Roles = UserRoles.Patient)]
    public async Task<IActionResult> GetMyRefills(CancellationToken cancellationToken)
    {
        var patientId = GetUserId();
        var result = await _appointmentService.GetPatientRefillsAsync(patientId, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPut("refills/{reminderId}/acknowledge")]
    [Authorize(Roles = UserRoles.Patient)]
    public async Task<IActionResult> AcknowledgeRefill(Guid reminderId, CancellationToken cancellationToken)
    {
        var patientId = GetUserId();
        var result = await _appointmentService.AcknowledgeRefillReminderAsync(reminderId, patientId, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("{appointmentId}/notes")]
    [Authorize(Roles = UserRoles.Doctor)]
    public async Task<IActionResult> SaveNotes(Guid appointmentId, [FromBody] string notes, CancellationToken cancellationToken)
    {
        var result = await _appointmentService.SaveConsultationNoteAsync(appointmentId, notes, cancellationToken);
        return ToActionResult(result);
    }

    private Guid GetUserId() => MediAlert.Extensions.ClaimsPrincipalExtensions.GetUserId(User);

    private IActionResult ToActionResult<T>(AppointmentServiceResult<T> result)
    {
        if (result.Succeeded) return StatusCode(result.StatusCode, result.Data);
        
        return StatusCode(result.StatusCode, new AppointmentsErrorResponse
        {
            Message = result.ErrorMessage ?? "Request failed.",
            ErrorCode = result.ErrorCode
        });
    }
}
