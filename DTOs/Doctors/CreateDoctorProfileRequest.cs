using System.ComponentModel.DataAnnotations;

namespace MediAlert.DTOs.Doctors;

public sealed class CreateDoctorProfileRequest
{
  [Required(ErrorMessage = "License number is required.")]
  [StringLength(100, MinimumLength = 3, ErrorMessage = "License number must be between 3 and 100 characters.")]
  public string LicenseNumber { get; set; } = string.Empty;

  [Required(ErrorMessage = "Specialization is required.")]
  [StringLength(200, ErrorMessage = "Specialization must not exceed 200 characters.")]
  public string Specialization { get; set; } = string.Empty;

  [Required(ErrorMessage = "Qualifications are required.")]
  [StringLength(500, ErrorMessage = "Qualifications must not exceed 500 characters.")]
  public string Qualifications { get; set; } = string.Empty;

  [Range(0, 80, ErrorMessage = "Experience years must be between 0 and 80.")]
  public int ExperienceYears { get; set; }
}
