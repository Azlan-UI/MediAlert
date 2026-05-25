namespace MediAlert.Models;

public class Patient
{
    public Guid PatientId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? PhoneNumber { get; set; }
    public int ComplianceStreakDays { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ApplicatioUser User { get; set; } = null!;
    public ICollection<Medication> Medications { get; set; } = [];
    public ICollection<IntakeLog> IntakeLogs { get; set; } = [];
    public ICollection<ComplianceReport> ComplianceReports { get; set; } = [];
}
