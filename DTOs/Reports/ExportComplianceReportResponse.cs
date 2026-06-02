namespace MediAlert.DTOs.Reports;

public sealed class ExportComplianceReportResponse
{
  public Guid ComplianceReportId { get; set; }

  public string PdfExportPath { get; set; } = string.Empty;

  public string? DownloadUrl { get; set; }

  public DateTime PdfExportedAt { get; set; }

  public string FileName { get; set; } = string.Empty;
}
