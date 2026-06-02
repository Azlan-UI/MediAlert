namespace MediAlert.DTOs.Appointments;

public sealed class AppointmentSummaryResponse
{
  public Guid AppointmentId { get; set; }

  public Guid PatientId { get; set; }

  public Guid? DoctorId { get; set; }

  public string AppointmentType { get; set; } = string.Empty;

  public DateTime ScheduledDateTime { get; set; }

  public string Status { get; set; } = string.Empty;

  public string? Location { get; set; }
}
