namespace MediAlert.DTOs.Doctors;

public sealed class DoctorAvailabilityResponse
{
  public Guid AvailabilityId { get; set; }

  public Guid DoctorId { get; set; }

  public int DayOfWeek { get; set; }

  public TimeOnly StartTime { get; set; }

  public TimeOnly EndTime { get; set; }

  public bool IsRecurring { get; set; }

  public bool IsActive { get; set; }

  public DateTime CreatedAt { get; set; }

  public DateTime? UpdatedAt { get; set; }
}
