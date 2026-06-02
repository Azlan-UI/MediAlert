namespace MediAlert.DTOs.Reports;

public sealed class ReportsErrorResponse
{
  public string Message { get; set; } = string.Empty;

  public string? ErrorCode { get; set; }

  public Dictionary<string, string[]>? Errors { get; set; }
}
