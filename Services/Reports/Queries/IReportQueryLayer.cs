using MediAlert.Models;

namespace MediAlert.Services.Reports.Queries;

public interface IReportQueryLayer
{
    Task<Patient?> GetPatientAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<bool> PatientExistsAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<List<IntakeLog>> GetIntakeLogsAsync(Guid patientId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);
    Task<List<Medication>> GetMedicationsAsync(Guid patientId, CancellationToken cancellationToken = default);
}
