namespace MediAlert.DTOs.Appointments;

public sealed class CalendarEventResponse
{
  public string EventType { get; set; } = string.Empty;

  public Guid EventId { get; set; }

  public string Title { get; set; } = string.Empty;

  public DateTime StartDateTime { get; set; }

  public DateTime? EndDateTime { get; set; }

  public string Status { get; set; } = string.Empty;

  public string? Location { get; set; }

  public Guid? DoctorId { get; set; }

  public Guid? MedicationId { get; set; }
}
