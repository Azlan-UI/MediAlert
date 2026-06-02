namespace MediAlert.DTOs.Caregiver;

public sealed class CaregiverMissedDoseAlertResponse
{
  public Guid IntakeLogId { get; set; }

  public Guid PatientId { get; set; }

  public Guid MedicationId { get; set; }

  public string MedicationName { get; set; } = string.Empty;

  public DateOnly ScheduledDate { get; set; }

  public TimeOnly ScheduledTime { get; set; }

  public string Status { get; set; } = string.Empty;

  public DateTime LoggedAt { get; set; }
}
