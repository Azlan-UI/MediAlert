namespace MediAlert.Models;

public class ComplianceReport
{
    public Guid ComplianceReportId { get; set; }
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
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    public Patient Patient { get; set; } = null!;
}
