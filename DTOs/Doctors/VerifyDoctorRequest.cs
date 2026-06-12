using System.ComponentModel.DataAnnotations;

namespace MediAlert.DTOs.Doctors;

/// <summary>
/// Admin verification decision (FR-23).
/// </summary>
public sealed class VerifyDoctorRequest
{
  [Required(ErrorMessage = "Verification status is required.")]
  [StringLength(20, ErrorMessage = "Verification status must not exceed 20 characters.")]
  public string VerificationStatus { get; set; } = string.Empty;

  [StringLength(500, ErrorMessage = "Rejection reason must not exceed 500 characters.")]
  public string? RejectionReason { get; set; }
}
