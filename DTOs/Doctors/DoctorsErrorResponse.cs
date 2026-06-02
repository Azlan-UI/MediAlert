namespace MediAlert.DTOs.Doctors;

public sealed class DoctorsErrorResponse
{
  public string Message { get; set; } = string.Empty;

  public string? ErrorCode { get; set; }

  public Dictionary<string, string[]>? Errors { get; set; }
}
