using System.ComponentModel.DataAnnotations;

namespace MediAlert.DTOs.Appointments;

public sealed class CreateAppointmentRequest
{
  [Required(ErrorMessage = "DoctorId is required.")]
  public Guid DoctorId { get; set; }

  [Required(ErrorMessage = "Appointment type is required.")]
  [StringLength(30, ErrorMessage = "Appointment type must not exceed 30 characters.")]
  public string AppointmentType { get; set; } = string.Empty;

  [Required(ErrorMessage = "Scheduled date and time is required.")]
  public DateTime ScheduledDateTime { get; set; }

  [Range(1, 480, ErrorMessage = "Duration must be between 1 and 480 minutes.")]
  public int DurationMinutes { get; set; } = 30;

  [StringLength(300, ErrorMessage = "Location must not exceed 300 characters.")]
  public string? Location { get; set; }

  [StringLength(1000, ErrorMessage = "Notes must not exceed 1000 characters.")]
  public string? Notes { get; set; }
}
