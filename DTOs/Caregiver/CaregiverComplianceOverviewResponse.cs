namespace MediAlert.DTOs.Caregiver;

public sealed class CaregiverComplianceOverviewResponse
{
  public Guid PatientId { get; set; }

  public string PatientFullName { get; set; } = string.Empty;

  public int Days { get; set; }

  public DateOnly PeriodStartDate { get; set; }

  public DateOnly PeriodEndDate { get; set; }

  public decimal OverallCompliancePercentage { get; set; }

  public int ComplianceStreakDays { get; set; }

  public int MissedDoseCount { get; set; }

  public List<CaregiverMedicationComplianceRowResponse> MedicationRows { get; set; } = [];
}
