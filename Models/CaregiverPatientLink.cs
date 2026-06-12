namespace MediAlert.Models;

public class CaregiverPatientLink
{
    public Guid CaregiverPatientLinkId { get; set; }
    public Guid CaregiverId { get; set; }
    public Guid PatientId { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
    public DateTime? ApprovedDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Caregiver Caregiver { get; set; } = null!;
    public Patient Patient { get; set; } = null!;
}
