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
[Authorize]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly IReportQueryLayer _queryLayer;
    private readonly IPdfExportService _pdfExportService;
    private readonly MediAlert.Data.ApplicationDbContext _dbContext;

    public ReportsController(IReportService reportService, IReportQueryLayer queryLayer, IPdfExportService pdfExportService, MediAlert.Data.ApplicationDbContext dbContext)
    {
        _reportService = reportService;
        _queryLayer = queryLayer;
        _pdfExportService = pdfExportService;
        _dbContext = dbContext;
    }

    [HttpGet("compliance")]
    public async Task<IActionResult> GetMonthlyCompliance([FromQuery] int month, [FromQuery] int year, CancellationToken cancellationToken, [FromQuery] Guid? patientId = null)
    {
        var targetPatientId = await ResolveTargetPatientIdAsync(patientId, cancellationToken);
        if (targetPatientId == null) return Forbid();

        var result = await _reportService.GenerateMonthlyComplianceReportAsync(targetPatientId.Value, month, year, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics(CancellationToken cancellationToken, [FromQuery] Guid? patientId = null)
    {
        var targetPatientId = await ResolveTargetPatientIdAsync(patientId, cancellationToken);
        if (targetPatientId == null) return Forbid();

        var result = await _reportService.GenerateStatisticsAsync(targetPatientId.Value, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("monthly/pdf")]
    public async Task<IActionResult> GetMonthlyCompliancePdf([FromQuery] int month, [FromQuery] int year, CancellationToken cancellationToken, [FromQuery] Guid? patientId = null)
    {
        var targetPatientId = await ResolveTargetPatientIdAsync(patientId, cancellationToken);
        if (targetPatientId == null) return Forbid();
        
        var patient = await _queryLayer.GetPatientAsync(targetPatientId.Value, cancellationToken);
        if (patient == null)
            return NotFound("Patient not found.");

        var result = await _reportService.GenerateMonthlyComplianceReportAsync(targetPatientId.Value, month, year, cancellationToken);
        if (!result.Succeeded || result.Data == null)
            return BadRequest(result.ErrorMessage);

        var pdfBytes = await _pdfExportService.GenerateComplianceReportPdfAsync(result.Data, patient);
        
        return File(pdfBytes, "application/pdf", $"ComplianceReport_{year}_{month:D2}.pdf");
    }

    private Guid GetUserId() => MediAlert.Extensions.ClaimsPrincipalExtensions.GetUserId(User);

    private async Task<Guid?> ResolveTargetPatientIdAsync(Guid? requestedPatientId, CancellationToken cancellationToken)
    {
        if (User.IsInRole(UserRoles.Doctor))
        {
            if (requestedPatientId == null) return null;
            var doctorUserId = GetUserId();
            var doctor = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(_dbContext.Doctors, d => d.UserId == doctorUserId.ToString(), cancellationToken);
            if (doctor == null) return null;
            
            var hasLink = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(_dbContext.PatientDoctorLinks, l => l.PatientId == requestedPatientId.Value && l.DoctorId == doctor.DoctorId, cancellationToken);
            return hasLink ? requestedPatientId.Value : null;
        }
        else if (User.IsInRole(UserRoles.Caregiver))
        {
            if (requestedPatientId == null) return null;
            var caregiverUserId = GetUserId();
            var caregiver = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(_dbContext.Caregivers, c => c.UserId == caregiverUserId.ToString(), cancellationToken);
            if (caregiver == null) return null;
            
            var hasLink = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(_dbContext.CaregiverPatientLinks, l => l.PatientId == requestedPatientId.Value && l.CaregiverId == caregiver.CaregiverId && l.Status == LinkStatuses.Approved, cancellationToken);
            return hasLink ? requestedPatientId.Value : null;
        }
        else
        {
            var userId = GetUserId().ToString();
            var patient = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(_dbContext.Patients, p => p.UserId == userId, cancellationToken);
            return patient?.PatientId;
        }
    }

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
