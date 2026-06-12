using System.ComponentModel.DataAnnotations;

namespace MediAlert.DTOs.Doctors;

public sealed class UpdateConsultationNoteRequest
{
  [StringLength(4000, ErrorMessage = "Diagnosis must not exceed 4000 characters.")]
  public string? Diagnosis { get; set; }

  [StringLength(4000, ErrorMessage = "Prescription must not exceed 4000 characters.")]
  public string? Prescription { get; set; }

  [StringLength(4000, ErrorMessage = "Observations must not exceed 4000 characters.")]
  public string? DoctorObservations { get; set; }
}
