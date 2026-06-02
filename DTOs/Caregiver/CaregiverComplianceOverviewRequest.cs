using System.ComponentModel.DataAnnotations;

namespace MediAlert.DTOs.Caregiver;

public sealed class CaregiverComplianceOverviewRequest
{
  [Required(ErrorMessage = "Patient ID is required.")]
  public Guid PatientId { get; set; }

  [Range(7, 30, ErrorMessage = "Overview period must be 7, 14, or 30 days.")]
  public int Days { get; set; } = 7;
}
