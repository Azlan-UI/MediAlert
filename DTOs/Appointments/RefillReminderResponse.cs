namespace MediAlert.DTOs.Appointments;

public sealed class RefillReminderResponse
{
  public Guid RefillReminderId { get; set; }

  public Guid MedicationId { get; set; }

  public Guid PatientId { get; set; }

  public string DrugName { get; set; } = string.Empty;

  public DateOnly RefillDueDate { get; set; }

  public int LeadTimeDays { get; set; }

  public bool IsAcknowledged { get; set; }

  public DateTime? AcknowledgedAt { get; set; }

  public bool IsActive { get; set; }

  public DateTime CreatedAt { get; set; }

  public DateTime? UpdatedAt { get; set; }
}
