namespace MediAlert.Models;

public class Caregiver
{
    public Guid CaregiverId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ApplicatioUser User { get; set; } = null!;
    public ICollection<CaregiverPatientLink> LinkedPatients { get; set; } = [];
}
