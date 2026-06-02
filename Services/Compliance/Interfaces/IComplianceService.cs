using MediAlert.DTOs.Compliance;

namespace MediAlert.Services.Compliance.Interfaces;

public interface IComplianceService
{
    Task<ComplianceServiceResult<IntakeLogResponse>> RecordIntakeAsync(
        RecordIntakeRequest request,
        CancellationToken cancellationToken = default);

    Task<ComplianceServiceResult<ComplianceHistoryResponse>> GetHistoryAsync(
        ComplianceHistoryRequest request,
        CancellationToken cancellationToken = default);

    Task<ComplianceServiceResult<ComplianceReportResponse>> GenerateReportAsync(
        GenerateComplianceReportRequest request,
        CancellationToken cancellationToken = default);
}
