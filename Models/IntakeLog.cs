namespace MediAlert.Models;

public class IntakeLog
{
    public Guid IntakeLogId { get; set; }
    public Guid PatientId { get; set; }
    public Guid DoseScheduleId { get; set; }
    public DateOnly ScheduledDate { get; set; }
    public TimeOnly ScheduledTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? ActualTakenAt { get; set; }
    public DateTime LoggedAt { get; set; } = DateTime.UtcNow;
    public string? SkippedReason { get; set; }
    public string? Notes { get; set; }

    public Patient Patient { get; set; } = null!;
    public DoseSchedule DoseSchedule { get; set; } = null!;
}
