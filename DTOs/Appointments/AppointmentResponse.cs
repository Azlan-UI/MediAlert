namespace MediAlert.DTOs.Appointments;

public sealed class AppointmentResponse
{
  public Guid AppointmentId { get; set; }

  public Guid PatientId { get; set; }

  public Guid? DoctorId { get; set; }

  public string AppointmentType { get; set; } = string.Empty;

  public DateTime ScheduledDateTime { get; set; }

  public int DurationMinutes { get; set; }

  public string? Location { get; set; }

  public string Status { get; set; } = string.Empty;

  public string? ZoomMeetingId { get; set; }

  public string? ZoomMeetingUrl { get; set; }

  public string? Notes { get; set; }

  public DateTime CreatedAt { get; set; }

  public DateTime? UpdatedAt { get; set; }
}
