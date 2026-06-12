using System.ComponentModel.DataAnnotations;

namespace MediAlert.DTOs.Appointments;

public sealed class CreateRefillReminderRequest
{
  [Required(ErrorMessage = "Medication ID is required.")]
  public Guid MedicationId { get; set; }

  [Required(ErrorMessage = "Refill due date is required.")]
  public DateOnly RefillDueDate { get; set; }

  [Range(0, 90, ErrorMessage = "Lead time must be between 0 and 90 days.")]
  public int LeadTimeDays { get; set; } = 7;
}
