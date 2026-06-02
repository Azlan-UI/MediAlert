namespace MediAlert.DTOs.Caregiver;

public sealed class CaregiverErrorResponse
{
  public string Message { get; set; } = string.Empty;

  public string? ErrorCode { get; set; }

  public Dictionary<string, string[]>? Errors { get; set; }
}
