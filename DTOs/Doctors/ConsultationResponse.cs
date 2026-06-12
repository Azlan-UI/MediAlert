namespace MediAlert.DTOs.Doctors;

public sealed class ConsultationResponse
{
  public Guid ConsultationId { get; set; }

  public Guid PatientId { get; set; }

  public Guid DoctorId { get; set; }

  public Guid? AppointmentId { get; set; }

  public Guid? DoctorAvailabilityId { get; set; }

  public DateTime ScheduledTime { get; set; }

  public int DurationMinutes { get; set; }

  public string? ZoomMeetingId { get; set; }

  public string? ZoomMeetingUrl { get; set; }

  public string Status { get; set; } = string.Empty;

  public bool IsFlagged { get; set; }

  public DateTime CreatedAt { get; set; }

  public DateTime? UpdatedAt { get; set; }
}
