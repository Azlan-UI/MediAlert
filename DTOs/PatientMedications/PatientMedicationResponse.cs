namespace MediAlert.DTOs.PatientMedications;

public sealed class PatientMedicationResponse
{
    public Guid PatientId { get; set; }
    public Guid MedicationId { get; set; }
    public string DrugName { get; set; } = string.Empty;
    public string DosageStrength { get; set; } = string.Empty;
    public string DosageForm { get; set; } = string.Empty;
    public int FrequencyPerDay { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsActive { get; set; }
    public List<PatientDoseScheduleResponse> DoseSchedules { get; set; } = [];
}
