using System.ComponentModel.DataAnnotations;

namespace MediAlert.DTOs.Doctors;

public sealed class CreateDoctorAvailabilityRequest
{
  [Range(0, 6, ErrorMessage = "Day of week must be between 0 (Sunday) and 6 (Saturday).")]
  public int? DayOfWeek { get; set; }

  public DateTime? SpecificDate { get; set; }

  [Required(ErrorMessage = "Start time is required.")]
  public TimeOnly StartTime { get; set; }

  [Required(ErrorMessage = "End time is required.")]
  public TimeOnly EndTime { get; set; }

  public bool IsRecurring { get; set; } = true;
}
