namespace MediAlert.DTOs.Appointments;

public sealed class RefillReminderDetailsResponse
{
  public Guid RefillReminderId { get; set; }

  public Guid MedicationId { get; set; }

  public Guid PatientId { get; set; }

  public string DrugName { get; set; } = string.Empty;

  public string DosageStrength { get; set; } = string.Empty;

  public string DosageForm { get; set; } = string.Empty;

  public string? PharmacyName { get; set; }

  public DateOnly RefillDueDate { get; set; }

  public int LeadTimeDays { get; set; }

  public DateOnly ReminderTriggerDate { get; set; }

  public bool IsAcknowledged { get; set; }

  public DateTime? AcknowledgedAt { get; set; }

  public bool IsActive { get; set; }

  public DateTime CreatedAt { get; set; }

  public DateTime? UpdatedAt { get; set; }
}
