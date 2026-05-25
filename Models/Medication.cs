namespace MediAlert.Models;

public class Medication
{
    public Guid MedicationId { get; set; }
    public Guid PatientId { get; set; }
    public string DrugName { get; set; } = string.Empty;
    public string DosageStrength { get; set; } = string.Empty;
    public string DosageForm { get; set; } = string.Empty;
    public int FrequencyPerDay { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? PrescribingPhysician { get; set; }
    public string? PharmacyName { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Patient Patient { get; set; } = null!;
    public ICollection<DoseSchedule> DoseSchedules { get; set; } = [];
}
