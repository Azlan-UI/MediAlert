namespace MediAlert.DTOs.Doctors;

public sealed class PatientDoctorLinkDetailsResponse
{
  public Guid LinkId { get; set; }

  public Guid PatientId { get; set; }

  public string PatientFullName { get; set; } = string.Empty;

  public string PatientEmail { get; set; } = string.Empty;

  public Guid DoctorId { get; set; }

  public string DoctorFullName { get; set; } = string.Empty;

  public string DoctorEmail { get; set; } = string.Empty;

  public string DoctorSpecialization { get; set; } = string.Empty;

  public string Status { get; set; } = string.Empty;

  public DateTime RequestedAt { get; set; }

  public DateTime? ApprovedAt { get; set; }

  public DateTime? RejectedAt { get; set; }
}
