namespace MediAlert.DTOs.Doctors;

public sealed class DoctorSummaryResponse
{
  public Guid DoctorId { get; set; }

  public string FullName { get; set; } = string.Empty;

  public string Specialization { get; set; } = string.Empty;

  public decimal? RatingAverage { get; set; }

  public bool IsVerified { get; set; }

  public int ExperienceYears { get; set; }

  public string LicenseNumber { get; set; } = string.Empty;

  public string Qualifications { get; set; } = string.Empty;
}
