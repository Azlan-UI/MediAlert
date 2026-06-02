namespace MediAlert.DTOs.Caregiver;

public sealed class CaregiverMedicationComplianceRowResponse
{
  public Guid MedicationId { get; set; }

  public string DrugName { get; set; } = string.Empty;

  public decimal CompliancePercentage { get; set; }

  public int TakenDoses { get; set; }

  public int MissedDoses { get; set; }

  public int DelayedDoses { get; set; }
}
