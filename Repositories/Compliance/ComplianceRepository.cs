using MediAlert.Data;
using MediAlert.Models;
using MediAlert.Repositories.Compliance.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MediAlert.Repositories.Compliance;

public sealed class ComplianceRepository : IComplianceRepository
{
    private readonly ApplicationDbContext _dbContext;

    public ComplianceRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Patient?> GetPatientAsync(
        Guid patientId,
        CancellationToken cancellationToken = default) =>
        _dbContext.Patients
            .AsTracking()
            .FirstOrDefaultAsync(patient => patient.PatientId == patientId, cancellationToken);

    public Task<DoseSchedule?> GetDoseScheduleForPatientAsync(
        Guid patientId,
        Guid doseScheduleId,
        CancellationToken cancellationToken = default) =>
        _dbContext.DoseSchedules
            .Include(schedule => schedule.Medication)
            .AsTracking()
            .FirstOrDefaultAsync(
                schedule =>
                    schedule.DoseScheduleId == doseScheduleId &&
                    schedule.Medication.PatientId == patientId,
                cancellationToken);

    public Task<IntakeLog?> GetIntakeLogAsync(
        Guid patientId,
        Guid doseScheduleId,
        DateOnly scheduledDate,
        CancellationToken cancellationToken = default) =>
        _dbContext.IntakeLogs
            .Include(log => log.DoseSchedule)
            .ThenInclude(schedule => schedule.Medication)
            .AsTracking()
            .FirstOrDefaultAsync(
                log =>
                    log.PatientId == patientId &&
                    log.DoseScheduleId == doseScheduleId &&
                    log.ScheduledDate == scheduledDate,
                cancellationToken);

    public Task<List<IntakeLog>> GetIntakeLogsForPeriodAsync(
        Guid patientId,
        DateOnly fromDate,
        DateOnly toDate,
        Guid? medicationId = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.IntakeLogs
            .Include(log => log.DoseSchedule)
            .ThenInclude(schedule => schedule.Medication)
            .AsNoTracking()
            .Where(log =>
                log.PatientId == patientId &&
                log.ScheduledDate >= fromDate &&
                log.ScheduledDate <= toDate);

        if (medicationId is not null)
        {
            query = query.Where(log => log.DoseSchedule.MedicationId == medicationId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(log => log.Status == status);
        }

        return query
            .OrderByDescending(log => log.ScheduledDate)
            .ThenBy(log => log.ScheduledTime)
            .ToListAsync(cancellationToken);
    }

    public Task<List<Medication>> GetMedicationsWithSchedulesForPeriodAsync(
        Guid patientId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default) =>
        _dbContext.Medications
            .Include(medication => medication.DoseSchedules)
            .AsNoTracking()
            .Where(medication =>
                medication.PatientId == patientId &&
                medication.StartDate <= toDate &&
                (medication.EndDate == null || medication.EndDate >= fromDate) &&
                medication.IsActive)
            .OrderBy(medication => medication.DrugName)
            .ToListAsync(cancellationToken);

    public Task<ComplianceReport?> GetComplianceReportAsync(
        Guid patientId,
        DateOnly periodStartDate,
        DateOnly periodEndDate,
        CancellationToken cancellationToken = default) =>
        _dbContext.ComplianceReports
            .AsTracking()
            .FirstOrDefaultAsync(
                report =>
                    report.PatientId == patientId &&
                    report.PeriodStartDate == periodStartDate &&
                    report.PeriodEndDate == periodEndDate,
                cancellationToken);

    public async Task AddIntakeLogAsync(
        IntakeLog intakeLog,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.IntakeLogs.AddAsync(intakeLog, cancellationToken);
    }

    public async Task AddComplianceReportAsync(
        ComplianceReport report,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.ComplianceReports.AddAsync(report, cancellationToken);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
