namespace MediAlert.DTOs.Doctors;

public sealed class ConsultationSummaryResponse
{
  public Guid ConsultationId { get; set; }

  public Guid PatientId { get; set; }

  public Guid DoctorId { get; set; }

  public string PatientFullName { get; set; } = string.Empty;

  public string DoctorFullName { get; set; } = string.Empty;

  public DateTime ScheduledTime { get; set; }

  public int DurationMinutes { get; set; }

  public string Status { get; set; } = string.Empty;

  public bool IsFlagged { get; set; }

  public string Type { get; set; } = string.Empty;

  public string? ZoomMeetingUrl { get; set; }
}
