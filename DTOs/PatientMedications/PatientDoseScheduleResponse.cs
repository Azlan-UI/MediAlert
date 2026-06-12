namespace MediAlert.DTOs.PatientMedications;

public sealed class PatientDoseScheduleResponse
{
    public Guid DoseScheduleId { get; set; }
    public TimeOnly ScheduledTime { get; set; }
    public int? DayOfWeek { get; set; }
    public bool IsActive { get; set; }
}
