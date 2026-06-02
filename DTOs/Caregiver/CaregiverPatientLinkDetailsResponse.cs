namespace MediAlert.DTOs.Caregiver;

public sealed class CaregiverPatientLinkDetailsResponse
{
  public Guid LinkId { get; set; }

  public Guid CaregiverId { get; set; }

  public Guid PatientId { get; set; }

  public string CaregiverFullName { get; set; } = string.Empty;

  public string CaregiverEmail { get; set; } = string.Empty;

  public string PatientFullName { get; set; } = string.Empty;

  public string PatientEmail { get; set; } = string.Empty;

  public string? PatientPhoneNumber { get; set; }

  public string PermissionLevel { get; set; } = string.Empty;

  public string Status { get; set; } = string.Empty;

  public string? RelationshipToPatient { get; set; }

  public string RequestedByUserId { get; set; } = string.Empty;

  public string RequestedByFullName { get; set; } = string.Empty;

  public DateTime RequestedAt { get; set; }

  public DateTime? ApprovedAt { get; set; }

  public DateTime? RejectedAt { get; set; }

  public int PatientComplianceStreakDays { get; set; }
}
