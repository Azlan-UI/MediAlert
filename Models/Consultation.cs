namespace MediAlert.Models;

public class Consultation
{
    public Guid ConsultationId { get; set; }
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public DateTime ScheduledDateTime { get; set; }
    public string Type { get; set; } = "Video"; // Video, InPerson
    public string? ZoomMeetingUrl { get; set; }
    public string Status { get; set; } = "Scheduled"; // Scheduled, Completed, Cancelled
    public bool IsFlagged { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Patient Patient { get; set; } = null!;
    public Doctor Doctor { get; set; } = null!;
    public ConsultationNote? ConsultationNote { get; set; }
}
