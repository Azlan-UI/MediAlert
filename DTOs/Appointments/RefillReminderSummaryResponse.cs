namespace MediAlert.DTOs.Appointments;

public sealed class RefillReminderSummaryResponse
{
  public Guid RefillReminderId { get; set; }

  public Guid MedicationId { get; set; }

  public string DrugName { get; set; } = string.Empty;

  public DateOnly RefillDueDate { get; set; }

  public int LeadTimeDays { get; set; }

  public bool IsAcknowledged { get; set; }

  public bool IsActive { get; set; }
}
