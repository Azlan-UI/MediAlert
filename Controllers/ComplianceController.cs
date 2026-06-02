using MediAlert.Constants;
using MediAlert.DTOs.Compliance;
using MediAlert.Services.Compliance.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediAlert.Controllers;

[ApiController]
[Authorize(Roles = UserRoles.Patient)]
[Route("api/compliance")]
[Produces("application/json")]
public class ComplianceController : ControllerBase
{
    private readonly IComplianceService _complianceService;

    public ComplianceController(IComplianceService complianceService)
    {
        _complianceService = complianceService;
    }

    /// <summary>
    /// Records or updates one intake log for a scheduled dose.
    /// </summary>
    /// <remarks>
    /// Status must be one of: Taken, Skipped, Missed, Delayed.
    /// If an intake log already exists for the same DoseScheduleId and ScheduledDate,
    /// it is updated instead of duplicated.
    /// </remarks>
    [HttpPost("intake-logs")]
    [ProducesResponseType(typeof(IntakeLogResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IntakeLogResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ComplianceErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ComplianceErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ComplianceErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ComplianceErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ComplianceErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ComplianceErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RecordIntake(
        [FromBody] RecordIntakeRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new ComplianceErrorResponse
            {
                Message = "Request body is required.",
                ErrorCode = ComplianceErrorCodes.InvalidRequest,
            });
        }

        var result = await _complianceService.RecordIntakeAsync(request, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Returns historical intake logs for a patient and date range.
    /// </summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(ComplianceHistoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ComplianceErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ComplianceErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ComplianceErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ComplianceErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ComplianceErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GetHistory(
        [FromQuery] Guid patientId,
        [FromQuery] DateOnly fromDate,
        [FromQuery] DateOnly toDate,
        [FromQuery] Guid? medicationId,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var result = await _complianceService.GetHistoryAsync(
            new ComplianceHistoryRequest
            {
                PatientId = patientId,
                FromDate = fromDate,
                ToDate = toDate,
                MedicationId = medicationId,
                Status = status,
            },
            cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Generates a compliance report for a patient and date range.
    /// </summary>
    /// <remarks>
    /// Missing logs for expected scheduled doses are counted as missed.
    /// The report can optionally include OpenFDA safety summaries for active medications.
    /// </remarks>
    [HttpPost("reports/generate")]
    [ProducesResponseType(typeof(ComplianceReportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ComplianceErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ComplianceErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ComplianceErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ComplianceErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ComplianceErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ComplianceErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GenerateReport(
        [FromBody] GenerateComplianceReportRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new ComplianceErrorResponse
            {
                Message = "Request body is required.",
                ErrorCode = ComplianceErrorCodes.InvalidRequest,
            });
        }

        var result = await _complianceService.GenerateReportAsync(request, cancellationToken);

        return ToActionResult(result);
    }

    private IActionResult ToActionResult<T>(ComplianceServiceResult<T> result)
    {
        if (result.Succeeded)
        {
            return StatusCode(result.StatusCode, result.Data);
        }

        var error = new ComplianceErrorResponse
        {
            Message = result.ErrorMessage ?? "Compliance request failed.",
            ErrorCode = result.ErrorCode,
            Errors = result.ValidationErrors,
        };

        return StatusCode(result.StatusCode, error);
    }
}
