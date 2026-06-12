namespace MediAlert.DTOs.Appointments;

public sealed class AppointmentDetailsResponse
{
  public Guid AppointmentId { get; set; }

  public Guid PatientId { get; set; }

  public string PatientFullName { get; set; } = string.Empty;

  public Guid? DoctorId { get; set; }

  public string? DoctorFullName { get; set; }

  public string? DoctorSpecialization { get; set; }

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
