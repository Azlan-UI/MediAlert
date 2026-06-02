namespace MediAlert.DTOs.Caregiver;

public sealed class CaregiverPatientLinkSummaryResponse
{
  public Guid LinkId { get; set; }

  public Guid CaregiverId { get; set; }

  public Guid PatientId { get; set; }

  public string PatientFullName { get; set; } = string.Empty;

  public string CaregiverFullName { get; set; } = string.Empty;

  public string PermissionLevel { get; set; } = string.Empty;

  public string Status { get; set; } = string.Empty;

  public DateTime RequestedAt { get; set; }
}
