using System.ComponentModel.DataAnnotations;

namespace MediAlert.DTOs.PatientMedications;

public class UpdatePatientMedicationRequest
{
    [Required]
    [MaxLength(100)]
    public string DrugName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string DosageStrength { get; set; } = string.Empty;

    [Required]
    public string DosageForm { get; set; } = string.Empty;

    [Range(1, 24)]
    public int FrequencyPerDay { get; set; }

    [Required]
    public string ScheduledTime { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    [MaxLength(100)]
    public string? PrescribingPhysician { get; set; }

    [MaxLength(100)]
    public string? PharmacyName { get; set; }
}
