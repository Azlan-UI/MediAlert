using MediAlert.Models;

namespace MediAlert.Repositories.Compliance.Interfaces;

public interface IComplianceRepository
{
    Task<Patient?> GetPatientAsync(
        Guid patientId,
        CancellationToken cancellationToken = default);

    Task<DoseSchedule?> GetDoseScheduleForPatientAsync(
        Guid patientId,
        Guid doseScheduleId,
        CancellationToken cancellationToken = default);

    Task<IntakeLog?> GetIntakeLogAsync(
        Guid patientId,
        Guid doseScheduleId,
        DateOnly scheduledDate,
        CancellationToken cancellationToken = default);

    Task<List<IntakeLog>> GetIntakeLogsForPeriodAsync(
        Guid patientId,
        DateOnly fromDate,
        DateOnly toDate,
        Guid? medicationId = null,
        string? status = null,
        CancellationToken cancellationToken = default);

    Task<List<Medication>> GetMedicationsWithSchedulesForPeriodAsync(
        Guid patientId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default);

    Task<ComplianceReport?> GetComplianceReportAsync(
        Guid patientId,
        DateOnly periodStartDate,
        DateOnly periodEndDate,
        CancellationToken cancellationToken = default);

    Task AddIntakeLogAsync(
        IntakeLog intakeLog,
        CancellationToken cancellationToken = default);

    Task AddComplianceReportAsync(
        ComplianceReport report,
        CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
