namespace MediAlert.DTOs.Appointments;

public sealed class CalendarEventsResponse
{
  public DateTime FromDateTime { get; set; }

  public DateTime ToDateTime { get; set; }

  public List<CalendarEventResponse> Events { get; set; } = [];
}
