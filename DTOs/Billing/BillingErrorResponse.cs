namespace MediAlert.DTOs.Billing;

public sealed class BillingErrorResponse
{
  public string Message { get; set; } = string.Empty;

  public string? ErrorCode { get; set; }

  public Dictionary<string, string[]>? Errors { get; set; }
}
