using System.ComponentModel.DataAnnotations;

namespace MediAlert.DTOs.Appointments;

public sealed class UpdateAppointmentRequest
{
  public Guid? DoctorId { get; set; }

  [StringLength(30, ErrorMessage = "Appointment type must not exceed 30 characters.")]
  public string? AppointmentType { get; set; }

  public DateTime? ScheduledDateTime { get; set; }

  [Range(1, 480, ErrorMessage = "Duration must be between 1 and 480 minutes.")]
  public int? DurationMinutes { get; set; }

  [StringLength(300, ErrorMessage = "Location must not exceed 300 characters.")]
  public string? Location { get; set; }

  [StringLength(20, ErrorMessage = "Status must not exceed 20 characters.")]
  public string? Status { get; set; }

  [StringLength(1000, ErrorMessage = "Notes must not exceed 1000 characters.")]
  public string? Notes { get; set; }
}
