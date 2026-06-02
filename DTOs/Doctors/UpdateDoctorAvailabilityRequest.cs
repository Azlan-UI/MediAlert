using System.ComponentModel.DataAnnotations;

namespace MediAlert.DTOs.Doctors;

public sealed class UpdateDoctorAvailabilityRequest
{
  [Range(0, 6, ErrorMessage = "Day of week must be between 0 (Sunday) and 6 (Saturday).")]
  public int? DayOfWeek { get; set; }

  public TimeOnly? StartTime { get; set; }

  public TimeOnly? EndTime { get; set; }

  public bool? IsRecurring { get; set; }

  public bool? IsActive { get; set; }
}
