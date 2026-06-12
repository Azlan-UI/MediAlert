namespace MediAlert.Models;

public class HealthCondition
{
    public Guid ConditionId { get; set; }
    public Guid PatientId { get; set; }
    public string ConditionName { get; set; } = string.Empty;
    public DateOnly? DiagnosedDate { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Patient Patient { get; set; } = null!;
}
