namespace MediAlert.DTOs.PatientMedications;

public sealed class AddPatientMedicationRequest
{
    public string DrugName { get; set; } = string.Empty;
    public string DosageStrength { get; set; } = string.Empty;
    public string DosageForm { get; set; } = "Tablet";
    public int FrequencyPerDay { get; set; } = 1;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string ScheduledTime { get; set; } = "09:00";
    public string? PrescribingPhysician { get; set; }
    public string? PharmacyName { get; set; }
}
