namespace MediAlert.DTOs.Doctors;

public sealed class DoctorAvailabilitySummaryResponse
{
  public Guid AvailabilityId { get; set; }

  public int? DayOfWeek { get; set; }

  public DateTime? SpecificDate { get; set; }

  public TimeOnly StartTime { get; set; }

  public TimeOnly EndTime { get; set; }

  public bool IsRecurring { get; set; }

  public bool IsActive { get; set; }
}
