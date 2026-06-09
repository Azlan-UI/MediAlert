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

    [HttpPut("{medicationId:guid}")]
    [ProducesResponseType(typeof(PatientMedicationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PatientMedicationErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(PatientMedicationErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(PatientMedicationErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(PatientMedicationErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EditMedicine(
        Guid medicationId,
        [FromBody] UpdatePatientMedicationRequest request,
        CancellationToken cancellationToken)
    {
        var errors = ValidateUpdateRequest(request);
        if (errors.Count > 0)
        {
            return BadRequest(new PatientMedicationErrorResponse
            {
                Message = "Medicine could not be updated because the request is invalid.",
                Errors = errors,
            });
        }

        var patient = await GetOrCreateCurrentPatientAsync(cancellationToken);
        if (patient is null) return Unauthorized();

        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync<IActionResult>(async () =>
        {
            // Clear change tracker to ensure a clean state on retry
            _dbContext.ChangeTracker.Clear();

            var medication = await _dbContext.Medications
                .FirstOrDefaultAsync(m => m.PatientId == patient.PatientId && m.MedicationId == medicationId, cancellationToken);

            if (medication == null) return NotFound();

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            medication.DrugName = request.DrugName.Trim();
            medication.DosageStrength = request.DosageStrength.Trim();
            medication.DosageForm = request.DosageForm.Trim();
            medication.FrequencyPerDay = request.FrequencyPerDay;
            medication.StartDate = request.StartDate;
            medication.EndDate = request.EndDate;
            medication.PrescribingPhysician = request.PrescribingPhysician?.Trim();
            medication.PharmacyName = request.PharmacyName?.Trim();
            medication.UpdatedAt = DateTime.UtcNow;

            // Soft-delete old schedules directly in the database
            await _dbContext.DoseSchedules
                .Where(ds => ds.MedicationId == medicationId && ds.IsActive)
                .ExecuteUpdateAsync(s => s.SetProperty(d => d.IsActive, false), cancellationToken);

            var firstDoseTime = ParseScheduledTime(request.ScheduledTime);
            medication.DoseSchedules = new List<DoseSchedule>(); // Initialize for MapMedication
            foreach (var scheduledTime in BuildDoseTimes(firstDoseTime, request.FrequencyPerDay))
            {
                var newSchedule = new DoseSchedule
                {
                    DoseScheduleId = Guid.NewGuid(),
                    MedicationId = medication.MedicationId,
                    ScheduledTime = scheduledTime,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                };
                _dbContext.DoseSchedules.Add(newSchedule);
                medication.DoseSchedules.Add(newSchedule);
            }

            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict updating medicine {MedicationId} for patient {PatientId}. Attempting to resolve by checking current database state.", medication.MedicationId, patient.PatientId);

                // Log detailed conflict information for troubleshooting
                foreach (var entry in ex.Entries)
                {
                    var databaseValues = await entry.GetDatabaseValuesAsync(cancellationToken);
                    if (databaseValues == null)
                    {
                        _logger.LogWarning("Entity {EntityType} with key {EntityKey} was deleted in the database.", 
                            entry.Entity.GetType().Name, 
                            entry.Metadata.FindPrimaryKey()?.Properties.Select(p => $"{p.Name}={entry.CurrentValues[p]}"));
                    }
                    else
                    {
                        _logger.LogWarning("Entity {EntityType} was modified. Database values differ from tracked values.", 
                            entry.Entity.GetType().Name);
                    }
                }

                // Verify if the medication still exists in the database
                var existingMedication = await _dbContext.Medications
                    .FirstOrDefaultAsync(m => m.PatientId == patient.PatientId && m.MedicationId == medicationId, cancellationToken);

                if (existingMedication == null)
                {
                    _logger.LogWarning("Medication {MedicationId} for patient {PatientId} no longer exists in the database.", medicationId, patient.PatientId);
                    return NotFound(new PatientMedicationErrorResponse
                    {
                        Message = "The medicine you are trying to update was deleted by another user. Please refresh your data.",
                        Errors = new Dictionary<string, string[]> { { "General", new[] { "Medication not found" } } }
                    });
                }

                // Clear the change tracker to reset the context
                _dbContext.ChangeTracker.Clear();

                // Return conflict status with user-friendly message
                return StatusCode(StatusCodes.Status409Conflict, new PatientMedicationErrorResponse
                {
                    Message = "The medicine was modified by another user or process. Please refresh your data and try again.",
                    Errors = new Dictionary<string, string[]> { { "General", new[] { "Concurrency conflict detected" } } }
                });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error occurred while updating medicine {MedicationId} for patient {PatientId}.", medication.MedicationId, patient.PatientId);
                return BadRequest(new PatientMedicationErrorResponse
                {
                    Message = "An error occurred while updating the medicine. Please try again.",
                    Errors = new Dictionary<string, string[]> { { "General", new[] { "Database operation failed" } } }
                });
            }

            _logger.LogInformation("Patient {PatientId} successfully updated medicine {MedicationId} ({DrugName}).", patient.PatientId, medication.MedicationId, medication.DrugName);
            return Ok(MapMedication(medication));
        });
    }

    [HttpDelete("{medicationId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(PatientMedicationErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(PatientMedicationErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(PatientMedicationErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeactivateMedicine(Guid medicationId, CancellationToken cancellationToken)
    {
        var patient = await GetOrCreateCurrentPatientAsync(cancellationToken);
        if (patient is null) return Unauthorized();

        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync<IActionResult>(async () =>
        {
            var medicationExists = await _dbContext.Medications
                .AnyAsync(m => m.PatientId == patient.PatientId && m.MedicationId == medicationId, cancellationToken);

            if (!medicationExists) return NotFound();

            try
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                await _dbContext.Medications
                    .Where(m => m.PatientId == patient.PatientId && m.MedicationId == medicationId)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(m => m.IsActive, false)
                        .SetProperty(m => m.UpdatedAt, DateTime.UtcNow), cancellationToken);

                await _dbContext.DoseSchedules
                    .Where(ds => ds.MedicationId == medicationId && ds.IsActive)
                    .ExecuteUpdateAsync(s => s.SetProperty(d => d.IsActive, false), cancellationToken);

                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database error deactivating medicine {MedicationId} for patient {PatientId}.", medicationId, patient.PatientId);
                return BadRequest(new PatientMedicationErrorResponse
                {
                    Message = "An error occurred while deactivating the medicine. Please try again.",
                    Errors = new Dictionary<string, string[]> { { "General", new[] { "Database operation failed" } } }
                });
            }

            _logger.LogInformation("Patient {PatientId} successfully deactivated medicine {MedicationId}.", patient.PatientId, medicationId);
            return NoContent();
        });
    }

    private static Dictionary<string, string[]> ValidateRequest(AddPatientMedicationRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.DrugName))
            errors[nameof(request.DrugName)] = new[] { "Medicine name is required." };

        if (string.IsNullOrWhiteSpace(request.DosageStrength))
            errors[nameof(request.DosageStrength)] = new[] { "Dosage is required." };

        if (request.FrequencyPerDay is < 1 or > 24)
            errors[nameof(request.FrequencyPerDay)] = new[] { "Frequency must be between 1 and 24." };

        var validForms = new[] { "Tablet", "Capsule", "Liquid", "Injection" };
        if (!validForms.Contains(request.DosageForm, StringComparer.OrdinalIgnoreCase))
            errors[nameof(request.DosageForm)] = new[] { "Dosage form must be Tablet, Capsule, Liquid, or Injection." };

        if (request.StartDate == default)
            errors[nameof(request.StartDate)] = new[] { "Start date is required." };

        if (request.EndDate is not null && request.EndDate < request.StartDate)
            errors[nameof(request.EndDate)] = new[] { "End date must be on or after start date." };

        if (!TimeOnly.TryParse(request.ScheduledTime, out _))
            errors[nameof(request.ScheduledTime)] = new[] { "Dose time must be a valid time." };

        return errors;
    }

    private static Dictionary<string, string[]> ValidateUpdateRequest(UpdatePatientMedicationRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.DrugName))
            errors[nameof(request.DrugName)] = new[] { "Medicine name is required." };

        if (string.IsNullOrWhiteSpace(request.DosageStrength))
            errors[nameof(request.DosageStrength)] = new[] { "Dosage is required." };

        if (request.FrequencyPerDay is < 1 or > 24)
            errors[nameof(request.FrequencyPerDay)] = new[] { "Frequency must be between 1 and 24." };

        var validForms = new[] { "Tablet", "Capsule", "Liquid", "Injection" };
        if (!validForms.Contains(request.DosageForm, StringComparer.OrdinalIgnoreCase))
            errors[nameof(request.DosageForm)] = new[] { "Dosage form must be Tablet, Capsule, Liquid, or Injection." };

        if (request.StartDate == default)
            errors[nameof(request.StartDate)] = new[] { "Start date is required." };

        if (request.EndDate is not null && request.EndDate < request.StartDate)
            errors[nameof(request.EndDate)] = new[] { "End date must be on or after start date." };

        if (!TimeOnly.TryParse(request.ScheduledTime, out _))
            errors[nameof(request.ScheduledTime)] = new[] { "Dose time must be a valid time." };

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
