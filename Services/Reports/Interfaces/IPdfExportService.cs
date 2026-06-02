using MediAlert.DTOs.Compliance;
using MediAlert.Models;

namespace MediAlert.Services.Reports.Interfaces;

public interface IPdfExportService
{
    Task<byte[]> GenerateComplianceReportPdfAsync(ComplianceReportResponse reportData, Patient patient);
}
