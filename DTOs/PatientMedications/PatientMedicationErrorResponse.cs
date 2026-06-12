namespace MediAlert.DTOs.PatientMedications;

public sealed class PatientMedicationErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, string[]>? Errors { get; set; }
}
