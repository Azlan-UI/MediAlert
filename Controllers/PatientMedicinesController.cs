using MediAlert.Constants;
using MediAlert.Data;
using MediAlert.DTOs.PatientMedications;
using MediAlert.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace MediAlert.Controllers;

[ApiController]
[Authorize(Roles = UserRoles.Patient)]
[Route("api/patient/medicines")]
[Produces("application/json")]
public class PatientMedicinesController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<PatientMedicinesController> _logger;

    public PatientMedicinesController(
        ApplicationDbContext dbContext,
        ILogger<PatientMedicinesController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PatientMedicationListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PatientMedicationErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMedicines(CancellationToken cancellationToken)
    {
        var patient = await GetOrCreateCurrentPatientAsync(cancellationToken);
        if (patient is null)
        {
            return Unauthorized(new PatientMedicationErrorResponse
            {
                Message = "Unable to resolve the current patient from the JWT token.",
            });
        }

        var medicines = await LoadMedicineResponsesAsync(patient.PatientId, cancellationToken);

        return Ok(new PatientMedicationListResponse
        {
            PatientId = patient.PatientId,
            Medicines = medicines,
        });
    }

    [HttpPost]
    [ProducesResponseType(typeof(PatientMedicationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(PatientMedicationErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(PatientMedicationErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AddMedicine(
        [FromBody] AddPatientMedicationRequest request,
        CancellationToken cancellationToken)
    {
        var errors = ValidateRequest(request);
        if (errors.Count > 0)
        {
            return BadRequest(new PatientMedicationErrorResponse
            {
                Message = "Medicine could not be saved because the request is invalid.",
                Errors = errors,
            });
        }

        var patient = await GetOrCreateCurrentPatientAsync(cancellationToken);
        if (patient is null)
        {
            return Unauthorized(new PatientMedicationErrorResponse
            {
                Message = "Unable to resolve the current patient from the JWT token.",
            });
        }

        var firstDoseTime = ParseScheduledTime(request.ScheduledTime);
        var medication = new Medication
        {
            MedicationId = Guid.NewGuid(),
            PatientId = patient.PatientId,
            DrugName = request.DrugName.Trim(),
            DosageStrength = request.DosageStrength.Trim(),
            DosageForm = string.IsNullOrWhiteSpace(request.DosageForm) ? "Tablet" : request.DosageForm.Trim(),
            FrequencyPerDay = request.FrequencyPerDay,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            PrescribingPhysician = string.IsNullOrWhiteSpace(request.PrescribingPhysician)
                ? null
                : request.PrescribingPhysician.Trim(),
            PharmacyName = string.IsNullOrWhiteSpace(request.PharmacyName)
                ? null
                : request.PharmacyName.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        foreach (var scheduledTime in BuildDoseTimes(firstDoseTime, request.FrequencyPerDay))
        {
            medication.DoseSchedules.Add(new DoseSchedule
            {
                DoseScheduleId = Guid.NewGuid(),
                MedicationId = medication.MedicationId,
                ScheduledTime = scheduledTime,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            });
        }

        _dbContext.Medications.Add(medication);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Patient {PatientId} added medicine {MedicationId} ({DrugName}).",
            patient.PatientId,
            medication.MedicationId,
            medication.DrugName);

        return StatusCode(StatusCodes.Status201Created, MapMedication(medication));
    }

    private Guid GetUserId() => MediAlert.Extensions.ClaimsPrincipalExtensions.GetUserId(User);

    private async Task<Patient?> GetOrCreateCurrentPatientAsync(CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        if (userId == Guid.Empty)
        {
            return null;
        }

        var patient = await _dbContext.Patients
            .FirstOrDefaultAsync(p => p.UserId == userId.ToString(), cancellationToken);

        if (patient is not null)
        {
            return patient;
        }

        patient = new Patient
        {
            PatientId = Guid.NewGuid(),
            UserId = userId.ToString(),
            CreatedAt = DateTime.UtcNow,
        };

        _dbContext.Patients.Add(patient);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return patient;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Concurrent patient profile creation detected for UserId {UserId}.", userId);

            return await _dbContext.Patients
                .FirstOrDefaultAsync(p => p.UserId == userId.ToString(), cancellationToken);
        }
    }

    private async Task<List<PatientMedicationResponse>> LoadMedicineResponsesAsync(
        Guid patientId,
        CancellationToken cancellationToken)
    {
        var medicines = await _dbContext.Medications
            .Include(m => m.DoseSchedules)
            .AsNoTracking()
            .Where(m => m.PatientId == patientId && m.IsActive)
            .OrderBy(m => m.DrugName)
            .ToListAsync(cancellationToken);

        return medicines.Select(MapMedication).ToList();
    }

    private static PatientMedicationResponse MapMedication(Medication medication) =>
        new()
        {
            PatientId = medication.PatientId,
            MedicationId = medication.MedicationId,
            DrugName = medication.DrugName,
            DosageStrength = medication.DosageStrength,
            DosageForm = medication.DosageForm,
            FrequencyPerDay = medication.FrequencyPerDay,
            StartDate = medication.StartDate,
            EndDate = medication.EndDate,
            IsActive = medication.IsActive,
            DoseSchedules = medication.DoseSchedules
                .Where(ds => ds.IsActive)
                .OrderBy(ds => ds.ScheduledTime)
                .Select(ds => new PatientDoseScheduleResponse
                {
                    DoseScheduleId = ds.DoseScheduleId,
                    ScheduledTime = ds.ScheduledTime,
                    DayOfWeek = ds.DayOfWeek,
                    IsActive = ds.IsActive,
                })
                .ToList(),
        };

    private static Dictionary<string, string[]> ValidateRequest(AddPatientMedicationRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.DrugName))
        {
            errors[nameof(request.DrugName)] = ["Medicine name is required."];
        }

        if (string.IsNullOrWhiteSpace(request.DosageStrength))
        {
            errors[nameof(request.DosageStrength)] = ["Dosage is required."];
        }

        if (request.FrequencyPerDay is < 1 or > 24)
        {
            errors[nameof(request.FrequencyPerDay)] = ["Frequency must be between 1 and 24."];
        }

        if (request.StartDate == default)
        {
            errors[nameof(request.StartDate)] = ["Start date is required."];
        }

        if (request.EndDate is not null && request.EndDate < request.StartDate)
        {
            errors[nameof(request.EndDate)] = ["End date must be on or after start date."];
        }

        if (!TimeOnly.TryParse(request.ScheduledTime, out _))
        {
            errors[nameof(request.ScheduledTime)] = ["Dose time must be a valid time."];
        }

        return errors;
    }

    private static TimeOnly ParseScheduledTime(string value) =>
        TimeOnly.TryParse(value, out var parsed)
            ? parsed
            : new TimeOnly(9, 0);

    private static IEnumerable<TimeOnly> BuildDoseTimes(TimeOnly firstDoseTime, int frequencyPerDay)
    {
        var intervalHours = 24.0 / frequencyPerDay;

        for (var index = 0; index < frequencyPerDay; index++)
        {
            yield return firstDoseTime.Add(TimeSpan.FromHours(intervalHours * index));
        }
    }
}
