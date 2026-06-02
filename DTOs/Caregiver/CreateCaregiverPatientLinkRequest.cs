using System.ComponentModel.DataAnnotations;

namespace MediAlert.DTOs.Caregiver;

/// <summary>
/// Patient-initiated request to link with a registered caregiver (FR-14).
/// </summary>
public sealed class CreateCaregiverPatientLinkRequest
{
  [Required(ErrorMessage = "Caregiver email is required.")]
  [EmailAddress(ErrorMessage = "Please provide a valid caregiver email address.")]
  [StringLength(256, ErrorMessage = "Email must not exceed 256 characters.")]
  public string CaregiverEmail { get; set; } = string.Empty;

  [Required(ErrorMessage = "Permission level is required.")]
  [StringLength(20, ErrorMessage = "Permission level must not exceed 20 characters.")]
  public string PermissionLevel { get; set; } = "ReadOnly";

  [StringLength(100, ErrorMessage = "Relationship must not exceed 100 characters.")]
  public string? RelationshipToPatient { get; set; }
}
