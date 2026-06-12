using System.ComponentModel.DataAnnotations;

namespace MediAlert.DTOs.Caregiver;

public sealed class RejectCaregiverPatientLinkRequest
{
  [StringLength(500, ErrorMessage = "Rejection reason must not exceed 500 characters.")]
  public string? Reason { get; set; }
}
