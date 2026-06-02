namespace MediAlert.DTOs.Compliance;

public sealed class ComplianceReportResponse
{
    public Guid? ComplianceReportId { get; set; }
    public Guid PatientId { get; set; }
    public DateOnly PeriodStartDate { get; set; }
    public DateOnly PeriodEndDate { get; set; }
    public int TotalScheduledDoses { get; set; }
    public int TakenDoses { get; set; }
    public int SkippedDoses { get; set; }
    public int MissedDoses { get; set; }
    public int DelayedDoses { get; set; }
    public decimal OverallCompliancePercentage { get; set; }
    public string? Recommendations { get; set; }
    public DateTime GeneratedAt { get; set; }
    public List<MedicationComplianceSummaryResponse> MedicationSummaries { get; set; } = [];
    public List<MedicationSafetySummaryResponse> OpenFdaSafetySummaries { get; set; } = [];
    public List<DailyTrendResponse> Trends { get; set; } = [];
}
