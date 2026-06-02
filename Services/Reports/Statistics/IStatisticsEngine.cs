using MediAlert.DTOs.Compliance;
using MediAlert.Models;

namespace MediAlert.Services.Reports.Statistics;

public interface IStatisticsEngine
{
    ComplianceReportResponse CalculateCompliance(
        Guid patientId, 
        DateOnly startDate, 
        DateOnly endDate, 
        List<IntakeLog> logs, 
        List<Medication> medications);
}
