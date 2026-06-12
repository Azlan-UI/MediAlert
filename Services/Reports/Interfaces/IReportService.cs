using MediAlert.DTOs.Compliance;

namespace MediAlert.Services.Reports.Interfaces;

public interface IReportService
{
    Task<ReportServiceResult<ComplianceReportResponse>> GenerateMonthlyComplianceReportAsync(Guid patientId, int month, int year, CancellationToken cancellationToken = default);
    Task<ReportServiceResult<ComplianceReportResponse>> GenerateStatisticsAsync(Guid patientId, CancellationToken cancellationToken = default);
}
