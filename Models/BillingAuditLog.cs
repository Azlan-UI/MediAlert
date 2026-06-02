namespace MediAlert.Models;

public class BillingAuditLog
{
    public Guid AuditId { get; set; }
    public Guid PatientId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Patient Patient { get; set; } = null!;
}
