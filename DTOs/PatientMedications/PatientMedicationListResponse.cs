namespace MediAlert.DTOs.PatientMedications;

public sealed class PatientMedicationListResponse
{
    public Guid PatientId { get; set; }
    public List<PatientMedicationResponse> Medicines { get; set; } = [];
}
