using System.ComponentModel.DataAnnotations;

namespace MediAlert.DTOs.Doctors;

public sealed class FlagConsultationRequest
{
  [StringLength(500, ErrorMessage = "Flag reason must not exceed 500 characters.")]
  public string? Reason { get; set; }
}
