using System.ComponentModel.DataAnnotations;

namespace MediAlert.DTOs.Doctors;

public sealed class RejectPatientDoctorLinkRequest
{
  [StringLength(500, ErrorMessage = "Rejection reason must not exceed 500 characters.")]
  public string? Reason { get; set; }
}
