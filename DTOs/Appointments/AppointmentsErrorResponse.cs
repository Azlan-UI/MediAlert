namespace MediAlert.DTOs.Appointments;

public sealed class AppointmentsErrorResponse
{
  public string Message { get; set; } = string.Empty;

  public string? ErrorCode { get; set; }

  public Dictionary<string, string[]>? Errors { get; set; }
}
