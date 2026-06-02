namespace MediAlert.DTOs.Doctors;

public sealed class PatientDoctorLinkSummaryResponse
{
  public Guid LinkId { get; set; }

  public Guid PatientId { get; set; }

  public Guid DoctorId { get; set; }

  public string PatientFullName { get; set; } = string.Empty;

  public string DoctorFullName { get; set; } = string.Empty;

  public string Status { get; set; } = string.Empty;

  public DateTime RequestedAt { get; set; }
}
