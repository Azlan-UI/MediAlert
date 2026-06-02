using System.ComponentModel.DataAnnotations;

namespace MediAlert.DTOs.Appointments;

public sealed class UpdateRefillReminderRequest
{
  public DateOnly? RefillDueDate { get; set; }

  [Range(0, 90, ErrorMessage = "Lead time must be between 0 and 90 days.")]
  public int? LeadTimeDays { get; set; }

  public bool? IsActive { get; set; }
}
