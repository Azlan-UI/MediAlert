using System.ComponentModel.DataAnnotations;

namespace MediAlert.DTOs.Reports;

public sealed class ExportComplianceReportRequest
{
  [StringLength(500, ErrorMessage = "Storage path hint must not exceed 500 characters.")]
  public string? PreferredFileName { get; set; }
}
