using System.ComponentModel.DataAnnotations;

namespace MediAlert.DTOs.Doctors;

public sealed class CreateConsultationRequest
{
  [Required(ErrorMessage = "Doctor ID is required.")]
  public Guid DoctorId { get; set; }

  public Guid? AppointmentId { get; set; }

  public Guid? DoctorAvailabilityId { get; set; }

  [Required(ErrorMessage = "Scheduled time is required.")]
  public DateTime ScheduledTime { get; set; }

  [Range(1, 480, ErrorMessage = "Duration must be between 1 and 480 minutes.")]
  public int DurationMinutes { get; set; } = 30;
}
