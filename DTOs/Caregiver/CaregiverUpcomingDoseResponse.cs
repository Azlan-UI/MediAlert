namespace MediAlert.DTOs.Caregiver;

public sealed class CaregiverUpcomingDoseResponse
{
  public Guid DoseScheduleId { get; set; }

  public Guid MedicationId { get; set; }

  public string MedicationName { get; set; } = string.Empty;

  public string DosageStrength { get; set; } = string.Empty;

  public DateOnly ScheduledDate { get; set; }

  public TimeOnly ScheduledTime { get; set; }

  public string? IntakeStatus { get; set; }
}
