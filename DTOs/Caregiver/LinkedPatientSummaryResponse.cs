namespace MediAlert.DTOs.Caregiver;

public sealed class LinkedPatientSummaryResponse
{
  public Guid PatientId { get; set; }

  public string FullName { get; set; } = string.Empty;

  public string Email { get; set; } = string.Empty;

  public int ComplianceStreakDays { get; set; }

  public decimal? RecentCompliancePercentage { get; set; }
}
