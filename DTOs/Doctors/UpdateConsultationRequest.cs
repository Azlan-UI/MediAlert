using System.ComponentModel.DataAnnotations;

namespace MediAlert.DTOs.Doctors;

public sealed class UpdateConsultationRequest
{
  public DateTime? ScheduledTime { get; set; }

  [Range(1, 480, ErrorMessage = "Duration must be between 1 and 480 minutes.")]
  public int? DurationMinutes { get; set; }

  [StringLength(30, ErrorMessage = "Status must not exceed 30 characters.")]
  public string? Status { get; set; }
}
