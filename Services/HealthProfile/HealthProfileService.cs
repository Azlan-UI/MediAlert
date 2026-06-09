using MediAlert.Data;
using MediAlert.DTOs.HealthProfile;
using MediAlert.Models;
using MediAlert.Services.HealthProfile.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MediAlert.Services.HealthProfile;

public class HealthProfileService : IHealthProfileService
{
    private readonly ApplicationDbContext _db;

    public HealthProfileService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<HealthConditionDto>> GetConditionsAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        return await _db.HealthConditions
            .AsNoTracking()
            .Where(hc => hc.PatientId == patientId)
            .OrderByDescending(hc => hc.DiagnosedDate)
            .Select(hc => new HealthConditionDto
            {
                ConditionId = hc.ConditionId,
                ConditionName = hc.ConditionName,
                DiagnosedDate = hc.DiagnosedDate,
                Notes = hc.Notes,
                CreatedAt = hc.CreatedAt,
                UpdatedAt = hc.UpdatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<HealthConditionDto?> GetConditionByIdAsync(Guid patientId, Guid conditionId, CancellationToken cancellationToken = default)
    {
        return await _db.HealthConditions
            .AsNoTracking()
            .Where(hc => hc.PatientId == patientId && hc.ConditionId == conditionId)
            .Select(hc => new HealthConditionDto
            {
                ConditionId = hc.ConditionId,
                ConditionName = hc.ConditionName,
                DiagnosedDate = hc.DiagnosedDate,
                Notes = hc.Notes,
                CreatedAt = hc.CreatedAt,
                UpdatedAt = hc.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<HealthConditionDto> AddConditionAsync(Guid patientId, CreateHealthConditionRequest request, CancellationToken cancellationToken = default)
    {
        var condition = new HealthCondition
        {
            PatientId = patientId,
            ConditionName = request.ConditionName,
            DiagnosedDate = request.DiagnosedDate,
            Notes = request.Notes
        };

        _db.HealthConditions.Add(condition);
        await _db.SaveChangesAsync(cancellationToken);

        return new HealthConditionDto
        {
            ConditionId = condition.ConditionId,
            ConditionName = condition.ConditionName,
            DiagnosedDate = condition.DiagnosedDate,
            Notes = condition.Notes,
            CreatedAt = condition.CreatedAt,
            UpdatedAt = condition.UpdatedAt
        };
    }

    public async Task<HealthConditionDto?> UpdateConditionAsync(Guid patientId, Guid conditionId, UpdateHealthConditionRequest request, CancellationToken cancellationToken = default)
    {
        var condition = await _db.HealthConditions
            .FirstOrDefaultAsync(hc => hc.PatientId == patientId && hc.ConditionId == conditionId, cancellationToken);

        if (condition == null)
            return null;

        condition.ConditionName = request.ConditionName;
        condition.DiagnosedDate = request.DiagnosedDate;
        condition.Notes = request.Notes;
        condition.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return new HealthConditionDto
        {
            ConditionId = condition.ConditionId,
            ConditionName = condition.ConditionName,
            DiagnosedDate = condition.DiagnosedDate,
            Notes = condition.Notes,
            CreatedAt = condition.CreatedAt,
            UpdatedAt = condition.UpdatedAt
        };
    }

    public async Task<bool> DeleteConditionAsync(Guid patientId, Guid conditionId, CancellationToken cancellationToken = default)
    {
        var condition = await _db.HealthConditions
            .FirstOrDefaultAsync(hc => hc.PatientId == patientId && hc.ConditionId == conditionId, cancellationToken);

        if (condition == null)
            return false;

        _db.HealthConditions.Remove(condition);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
