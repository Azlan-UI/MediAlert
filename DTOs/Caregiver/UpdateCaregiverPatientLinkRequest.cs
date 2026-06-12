using System.ComponentModel.DataAnnotations;

namespace MediAlert.DTOs.Caregiver;

public sealed class UpdateCaregiverPatientLinkRequest
{
  [StringLength(20, ErrorMessage = "Permission level must not exceed 20 characters.")]
  public string? PermissionLevel { get; set; }

  [StringLength(100, ErrorMessage = "Relationship must not exceed 100 characters.")]
  public string? RelationshipToPatient { get; set; }
}
