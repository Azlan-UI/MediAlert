namespace MediAlert.Models;

public class DoctorAvailability
{
    public Guid DoctorAvailabilityId { get; set; }
    public Guid DoctorId { get; set; }
    public DayOfWeek? DayOfWeek { get; set; }
    public DateTime? SpecificDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Doctor Doctor { get; set; } = null!;
}
