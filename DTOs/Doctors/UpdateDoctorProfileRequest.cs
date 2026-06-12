using System.ComponentModel.DataAnnotations;

namespace MediAlert.DTOs.Doctors;

public sealed class UpdateDoctorProfileRequest
{
  [StringLength(200, ErrorMessage = "Specialization must not exceed 200 characters.")]
  public string? Specialization { get; set; }

  [StringLength(500, ErrorMessage = "Qualifications must not exceed 500 characters.")]
  public string? Qualifications { get; set; }

  [Range(0, 80, ErrorMessage = "Experience years must be between 0 and 80.")]
  public int? ExperienceYears { get; set; }
}
