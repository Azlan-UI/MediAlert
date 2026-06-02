namespace MediAlert.DTOs.Compliance;

public sealed class ComplianceHistoryResponse
{
    public Guid PatientId { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public int TotalLogs { get; set; }
    public List<IntakeLogResponse> Logs { get; set; } = [];
}
