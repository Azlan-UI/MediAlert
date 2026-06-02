namespace MediAlert.DTOs.Compliance;

public sealed class GenerateComplianceReportRequest
{
    public Guid PatientId { get; set; }
    public DateOnly PeriodStartDate { get; set; }
    public DateOnly PeriodEndDate { get; set; }
    public bool IncludeOpenFdaSafetySummary { get; set; } = true;
    public bool PersistReport { get; set; } = true;
}
