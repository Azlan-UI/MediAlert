namespace MediAlert.Models;

public class PatientDoctorLink
{
    public Guid PatientDoctorLinkId { get; set; }
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
    public DateTime? ApprovedDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Patient Patient { get; set; } = null!;
    public Doctor Doctor { get; set; } = null!;
}
