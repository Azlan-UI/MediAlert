namespace MediAlert.DTOs.Compliance;

public sealed class IntakeLogResponse
{
    public Guid IntakeLogId { get; set; }
    public Guid PatientId { get; set; }
    public Guid MedicationId { get; set; }
    public string MedicationName { get; set; } = string.Empty;
    public Guid DoseScheduleId { get; set; }
    public DateOnly ScheduledDate { get; set; }
    public TimeOnly ScheduledTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? ActualTakenAt { get; set; }
    public DateTime LoggedAt { get; set; }
    public string? SkippedReason { get; set; }
    public string? Notes { get; set; }
}
