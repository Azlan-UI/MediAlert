using System.ComponentModel.DataAnnotations;
using MediAlert.DTOs.Common;

namespace MediAlert.DTOs.Appointments;

/// <summary>
/// Unified calendar view for appointments and refill reminders (FR-17, FR-18).
/// </summary>
public sealed class CalendarSearchRequest : PaginationRequest
{
  [Required(ErrorMessage = "Range start is required.")]
  public DateTime FromDateTime { get; set; }

  [Required(ErrorMessage = "Range end is required.")]
  public DateTime ToDateTime { get; set; }

  public bool IncludeAppointments { get; set; } = true;

  public bool IncludeRefillReminders { get; set; } = true;
}
