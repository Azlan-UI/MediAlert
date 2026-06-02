namespace MediAlert.Models;

public class ConsultationNote
{
    public Guid ConsultationNoteId { get; set; }
    public Guid ConsultationId { get; set; }
    public string ClinicalNotes { get; set; } = string.Empty;
    public string? Prescriptions { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Consultation Consultation { get; set; } = null!;
}
