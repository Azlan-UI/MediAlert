namespace MediAlert.Models;

public class Doctor
{
    public Guid DoctorId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public string Qualifications { get; set; } = string.Empty;
    public int YearsOfExperience { get; set; }
    public string VerificationStatus { get; set; } = "Pending"; // Pending, Verified, Rejected
    public string? RejectionReason { get; set; }
    public string? ClinicName { get; set; }
    public string? ContactInfo { get; set; }
    public string? Biography { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


    public ApplicatioUser User { get; set; } = null!;
    public ICollection<DoctorAvailability> Availabilities { get; set; } = [];
    public ICollection<PatientDoctorLink> LinkedPatients { get; set; } = [];
    public ICollection<Consultation> Consultations { get; set; } = [];
    public ICollection<Subscription> Subscriptions { get; set; } = [];
}
