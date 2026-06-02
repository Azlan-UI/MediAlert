namespace MediAlert.DTOs.Compliance;

public sealed class MedicationComplianceSummaryResponse
{
    public Guid MedicationId { get; set; }
    public string DrugName { get; set; } = string.Empty;
    public int TotalScheduledDoses { get; set; }
    public int TakenDoses { get; set; }
    public int SkippedDoses { get; set; }
    public int MissedDoses { get; set; }
    public int DelayedDoses { get; set; }
    public decimal CompliancePercentage { get; set; }
}
