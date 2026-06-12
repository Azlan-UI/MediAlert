namespace MediAlert.Models;

public class DoseSchedule
{
    public Guid DoseScheduleId { get; set; }
    public Guid MedicationId { get; set; }
    public TimeOnly ScheduledTime { get; set; }
    public int? DayOfWeek { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Medication Medication { get; set; } = null!;
    public ICollection<IntakeLog> IntakeLogs { get; set; } = [];
}
