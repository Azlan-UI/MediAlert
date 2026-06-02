using MediAlert.Data;
using MediAlert.Models;
using Microsoft.EntityFrameworkCore;

namespace MediAlert.Services.Reports.Queries;

public sealed class ReportQueryLayer : IReportQueryLayer
{
    private readonly ApplicationDbContext _db;

    public ReportQueryLayer(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Patient?> GetPatientAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        return await _db.Patients
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.PatientId == patientId, cancellationToken);
    }

    public async Task<bool> PatientExistsAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        return await _db.Patients.AnyAsync(p => p.PatientId == patientId, cancellationToken);
    }

    public async Task<List<IntakeLog>> GetIntakeLogsAsync(Guid patientId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        return await _db.IntakeLogs
            .AsNoTracking()
            .Include(il => il.DoseSchedule)
            .Where(il => il.PatientId == patientId && il.ScheduledDate >= startDate && il.ScheduledDate <= endDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Medication>> GetMedicationsAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        return await _db.Medications
            .AsNoTracking()
            .Include(m => m.DoseSchedules)
            .Where(m => m.PatientId == patientId)
            .ToListAsync(cancellationToken);
    }
}
