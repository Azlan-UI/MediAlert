using System.ComponentModel.DataAnnotations;

namespace MediAlert.DTOs.Appointments;

public sealed class CancelAppointmentRequest
{
  [StringLength(500, ErrorMessage = "Cancellation reason must not exceed 500 characters.")]
  public string? Reason { get; set; }
}
