using System.ComponentModel.DataAnnotations;

namespace MediAlert.DTOs.Doctors;

public sealed class CancelConsultationRequest
{
  [StringLength(500, ErrorMessage = "Cancellation reason must not exceed 500 characters.")]
  public string? Reason { get; set; }
}
