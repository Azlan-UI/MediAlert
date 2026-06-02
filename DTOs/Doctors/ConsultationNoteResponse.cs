namespace MediAlert.DTOs.Doctors;

public sealed class ConsultationNoteResponse
{
  public Guid NoteId { get; set; }

  public Guid ConsultationId { get; set; }

  public string? Diagnosis { get; set; }

  public string? Prescription { get; set; }

  public string? DoctorObservations { get; set; }

  public DateTime CreatedAt { get; set; }

  public DateTime? UpdatedAt { get; set; }
}
