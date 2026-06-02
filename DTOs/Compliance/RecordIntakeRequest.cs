namespace MediAlert.DTOs.Compliance;

public sealed class RecordIntakeRequest
{
    public Guid PatientId { get; set; }
    public Guid DoseScheduleId { get; set; }
    public DateOnly ScheduledDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? ActualTakenAt { get; set; }
    public string? SkippedReason { get; set; }
    public string? Notes { get; set; }
}
