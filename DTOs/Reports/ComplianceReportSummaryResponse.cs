namespace MediAlert.DTOs.Reports;

public sealed class ComplianceReportSummaryResponse
{
  public Guid ComplianceReportId { get; set; }

  public Guid PatientId { get; set; }

  public DateOnly PeriodStartDate { get; set; }

  public DateOnly PeriodEndDate { get; set; }

  public decimal OverallCompliancePercentage { get; set; }

  public int TotalScheduledDoses { get; set; }

  public int TakenDoses { get; set; }

  public int MissedDoses { get; set; }

  public DateTime GeneratedAt { get; set; }

  public bool HasPdfExport { get; set; }
}
