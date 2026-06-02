namespace MediAlert.DTOs.Doctors;

public sealed class PatientDoctorLinkResponse
{
  public Guid LinkId { get; set; }

  public Guid PatientId { get; set; }

  public Guid DoctorId { get; set; }

  public string Status { get; set; } = string.Empty;

  public DateTime RequestedAt { get; set; }

  public DateTime? ApprovedAt { get; set; }

  public DateTime? RejectedAt { get; set; }
}
