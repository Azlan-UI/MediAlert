using System.Security.Claims;
using MediAlert.Constants;
using MediAlert.DTOs.Compliance;
using MediAlert.Services.Reports;
using MediAlert.Services.Reports.Interfaces;
using MediAlert.Services.Reports.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediAlert.Controllers;

[ApiController]
[Authorize(Roles = UserRoles.Patient)]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly IReportQueryLayer _queryLayer;
    private readonly IPdfExportService _pdfExportService;

    public ReportsController(IReportService reportService, IReportQueryLayer queryLayer, IPdfExportService pdfExportService)
    {
        _reportService = reportService;
        _queryLayer = queryLayer;
        _pdfExportService = pdfExportService;
    }

    [HttpGet("compliance")]
    public async Task<IActionResult> GetMonthlyCompliance([FromQuery] int month, [FromQuery] int year, CancellationToken cancellationToken)
    {
        var patientId = GetUserId();
        var result = await _reportService.GenerateMonthlyComplianceReportAsync(patientId, month, year, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("statistics")]
    [Authorize(Policy = AuthorizationPolicies.PremiumPatient)]
    public async Task<IActionResult> GetStatistics(CancellationToken cancellationToken)
    {
        var patientId = GetUserId();
        var result = await _reportService.GenerateStatisticsAsync(patientId, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("monthly/pdf")]
    [Authorize(Policy = AuthorizationPolicies.PremiumPatient)]
    [Produces("application/pdf")]
    public async Task<IActionResult> GetMonthlyCompliancePdf([FromQuery] int month, [FromQuery] int year, CancellationToken cancellationToken)
    {
        var patientId = GetUserId();
        
        var patient = await _queryLayer.GetPatientAsync(patientId, cancellationToken);
        if (patient == null)
            return NotFound("Patient not found.");

        var result = await _reportService.GenerateMonthlyComplianceReportAsync(patientId, month, year, cancellationToken);
        if (!result.Succeeded || result.Data == null)
            return BadRequest(result.ErrorMessage);

        var pdfBytes = await _pdfExportService.GenerateComplianceReportPdfAsync(result.Data, patient);
        
        return File(pdfBytes, "application/pdf", $"ComplianceReport_{year}_{month:D2}.pdf");
    }

    private Guid GetUserId() => MediAlert.Extensions.ClaimsPrincipalExtensions.GetUserId(User);

    private IActionResult ToActionResult<T>(ReportServiceResult<T> result)
    {
        if (result.Succeeded) return StatusCode(result.StatusCode, result.Data);
        
        return StatusCode(result.StatusCode, new ComplianceErrorResponse
        {
            Message = result.ErrorMessage ?? "Request failed.",
            ErrorCode = result.ErrorCode
        });
    }
}
