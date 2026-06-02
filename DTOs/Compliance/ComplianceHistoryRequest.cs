namespace MediAlert.DTOs.Compliance;

public sealed class ComplianceHistoryRequest
{
    public Guid PatientId { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public Guid? MedicationId { get; set; }
    public string? Status { get; set; }
}
