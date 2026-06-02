using MediAlert.DTOs.Compliance;

namespace MediAlert.DTOs.Reports;

public sealed class ComplianceReportDetailsResponse
{
  public Guid ComplianceReportId { get; set; }

  public Guid PatientId { get; set; }

  public string PatientFullName { get; set; } = string.Empty;

  public DateOnly PeriodStartDate { get; set; }

  public DateOnly PeriodEndDate { get; set; }

  public int TotalScheduledDoses { get; set; }

  public int TakenDoses { get; set; }

  public int SkippedDoses { get; set; }

  public int MissedDoses { get; set; }

  public int DelayedDoses { get; set; }

  public decimal OverallCompliancePercentage { get; set; }

  public string? Recommendations { get; set; }

  public Guid? MostMissedMedicationId { get; set; }

  public string? MostMissedMedicationName { get; set; }

  public string? MostMissedPatternNote { get; set; }

  public DateTime GeneratedAt { get; set; }

  public string? PdfExportPath { get; set; }

  public DateTime? PdfExportedAt { get; set; }

  public List<MedicationComplianceSummaryResponse> MedicationSummaries { get; set; } = [];
}
