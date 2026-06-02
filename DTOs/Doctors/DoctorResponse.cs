namespace MediAlert.DTOs.Doctors;

public sealed class DoctorResponse
{
  public Guid DoctorId { get; set; }

  public string UserId { get; set; } = string.Empty;

  public string FullName { get; set; } = string.Empty;

  public string Email { get; set; } = string.Empty;

  public string LicenseNumber { get; set; } = string.Empty;

  public string Specialization { get; set; } = string.Empty;

  public string Qualifications { get; set; } = string.Empty;

  public int ExperienceYears { get; set; }

  public decimal? RatingAverage { get; set; }

  public bool IsVerified { get; set; }

  public string VerificationStatus { get; set; } = string.Empty;

  public DateTime CreatedAt { get; set; }

  public DateTime? UpdatedAt { get; set; }
}
