using MediAlert.Constants;
using MediAlert.DTOs.OpenFda;
using MediAlert.Services.OpenFda.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Security.Claims;

namespace MediAlert.Controllers;

[ApiController]
[Authorize]
[Route("api/openfda/drugs")]
[Produces("application/json")]
public class OpenFdaController : ControllerBase
{
    private readonly IOpenFdaDrugClient _openFdaDrugClient;
    private readonly MediAlert.Data.ApplicationDbContext _dbContext;

    public OpenFdaController(IOpenFdaDrugClient openFdaDrugClient, MediAlert.Data.ApplicationDbContext dbContext)
    {
        _openFdaDrugClient = openFdaDrugClient;
        _dbContext = dbContext;
    }

    [HttpGet("search")]
    [ProducesResponseType(typeof(OpenFdaDrugSearchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Search(
        [FromQuery] string query,
        [FromQuery] int? limit,
        [FromQuery] int? skip,
        CancellationToken cancellationToken)
    {
        var result = await _openFdaDrugClient.SearchDrugLabelsAsync(
            new OpenFdaDrugSearchRequest
            {
                Query = query,
                Limit = limit,
                Skip = skip,
            },
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpGet("labels/raw")]
    [ProducesResponseType(typeof(OpenFdaLabelApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetRawLabel(
        [FromQuery] string query,
        [FromQuery] int? limit,
        CancellationToken cancellationToken)
    {
        var result = await _openFdaDrugClient.GetRawDrugLabelAsync(
            query,
            limit,
            cancellationToken);

        return ToActionResult(result);
    }

    private IActionResult ToActionResult<T>(OpenFdaClientResult<T> result)
    {
        if (result.Succeeded)
        {
            return Ok(result.Data);
        }

        if (result.RetryAfter is not null)
        {
            Response.Headers.RetryAfter = Math.Ceiling(result.RetryAfter.Value.TotalSeconds)
                .ToString("0");
        }

        var error = new
        {
            result.ErrorCode,
            result.ErrorMessage,
        };

        return result.ErrorCode switch
        {
            OpenFdaErrorCodes.InvalidRequest => BadRequest(error),
            OpenFdaErrorCodes.RateLimited => StatusCode(StatusCodes.Status429TooManyRequests, error),
            OpenFdaErrorCodes.Timeout => StatusCode(StatusCodes.Status503ServiceUnavailable, error),
            OpenFdaErrorCodes.Unavailable => StatusCode(StatusCodes.Status503ServiceUnavailable, error),
            OpenFdaErrorCodes.InvalidResponse => StatusCode(StatusCodes.Status502BadGateway, error),
            _ when result.StatusCode == HttpStatusCode.NotFound => NotFound(error),
            _ => StatusCode(StatusCodes.Status502BadGateway, error),
        };
    }

    [HttpPost("reports")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> SaveReport([FromBody] SaveInteractionReportRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var patient = await _dbContext.Patients.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        if (patient == null) return Unauthorized();

        var report = new MediAlert.Models.InteractionReport
        {
            PatientId = patient.PatientId,
            QueryDrugName = request.QueryDrugName,
            ExistingDrugNames = request.ExistingDrugNames,
            SeverityLevel = request.SeverityLevel,
            ExplanationText = request.ExplanationText,
            GeneratedAt = DateTime.UtcNow,
            IsSaved = true
        };

        _dbContext.InteractionReports.Add(report);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { Message = "Report saved successfully.", ReportId = report.ReportId });
    }

    [HttpGet("reports")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> GetReports(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var patient = await _dbContext.Patients.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        if (patient == null) return Unauthorized();

        var reports = await _dbContext.InteractionReports
            .Where(r => r.PatientId == patient.PatientId && r.IsSaved)
            .OrderByDescending(r => r.GeneratedAt)
            .ToListAsync(cancellationToken);

        return Ok(reports);
    }
}
