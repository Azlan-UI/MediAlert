using System.ComponentModel.DataAnnotations;

namespace MediAlert.DTOs.Reports;

/// <summary>
/// Generate a report for a calendar month (FR-19).
/// </summary>
public sealed class GenerateMonthlyReportRequest
{
  [Required(ErrorMessage = "Patient ID is required.")]
  public Guid PatientId { get; set; }

  [Range(1, 12, ErrorMessage = "Month must be between 1 and 12.")]
  public int Month { get; set; }

  [Range(2000, 2100, ErrorMessage = "Year must be between 2000 and 2100.")]
  public int Year { get; set; }

  public bool IncludeOpenFdaSafetySummary { get; set; }

  public bool PersistReport { get; set; } = true;
}
