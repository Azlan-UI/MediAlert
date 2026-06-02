using MediAlert.DTOs.Compliance;
using MediAlert.Services.Reports.Interfaces;
using MediAlert.Services.Reports.Queries;
using MediAlert.Services.Reports.Statistics;

namespace MediAlert.Services.Reports;

public sealed class ReportService : IReportService
{
    private readonly IReportQueryLayer _queryLayer;
    private readonly IStatisticsEngine _statisticsEngine;

    public ReportService(IReportQueryLayer queryLayer, IStatisticsEngine statisticsEngine)
    {
        _queryLayer = queryLayer;
        _statisticsEngine = statisticsEngine;
    }

    public async Task<ReportServiceResult<ComplianceReportResponse>> GenerateMonthlyComplianceReportAsync(Guid patientId, int month, int year, CancellationToken cancellationToken = default)
    {
        if (!await _queryLayer.PatientExistsAsync(patientId, cancellationToken))
            return ReportServiceResult<ComplianceReportResponse>.Failure("patient_not_found", "Patient not found");

        var startDate = new DateOnly(year, month, 1);
        var endDate = new DateOnly(year, month, DateTime.DaysInMonth(year, month));

        var logs = await _queryLayer.GetIntakeLogsAsync(patientId, startDate, endDate, cancellationToken);
        var medications = await _queryLayer.GetMedicationsAsync(patientId, cancellationToken);

        var report = _statisticsEngine.CalculateCompliance(patientId, startDate, endDate, logs, medications);
        
        return ReportServiceResult<ComplianceReportResponse>.Success(report);
    }

    public async Task<ReportServiceResult<ComplianceReportResponse>> GenerateStatisticsAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        if (!await _queryLayer.PatientExistsAsync(patientId, cancellationToken))
            return ReportServiceResult<ComplianceReportResponse>.Failure("patient_not_found", "Patient not found");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var startDate = today.AddDays(-30);
        var endDate = today;

        var logs = await _queryLayer.GetIntakeLogsAsync(patientId, startDate, endDate, cancellationToken);
        var medications = await _queryLayer.GetMedicationsAsync(patientId, cancellationToken);

        var report = _statisticsEngine.CalculateCompliance(patientId, startDate, endDate, logs, medications);
        
        return ReportServiceResult<ComplianceReportResponse>.Success(report);
    }
}
