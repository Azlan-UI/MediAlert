namespace MediAlert.DTOs.Doctors;

public sealed class ConsultationDetailsResponse
{
  public Guid ConsultationId { get; set; }

  public Guid PatientId { get; set; }

  public string PatientFullName { get; set; } = string.Empty;

  public string PatientEmail { get; set; } = string.Empty;

  public Guid DoctorId { get; set; }

  public string DoctorFullName { get; set; } = string.Empty;

  public string DoctorSpecialization { get; set; } = string.Empty;

  public Guid? AppointmentId { get; set; }

  public Guid? DoctorAvailabilityId { get; set; }

  public DateTime ScheduledTime { get; set; }

  public string Type { get; set; } = string.Empty;

  public int DurationMinutes { get; set; }

  public string? ZoomMeetingId { get; set; }

  public string? ZoomMeetingUrl { get; set; }

  public string Status { get; set; } = string.Empty;

  public bool IsFlagged { get; set; }

  public DateTime? FlaggedAt { get; set; }

  public DateTime? CancelledAt { get; set; }

  public ConsultationNoteResponse? Note { get; set; }

  public DateTime CreatedAt { get; set; }

  public DateTime? UpdatedAt { get; set; }
}
