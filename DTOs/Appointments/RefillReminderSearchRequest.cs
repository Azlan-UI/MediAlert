using MediAlert.DTOs.Common;

namespace MediAlert.DTOs.Appointments;

public sealed class RefillReminderSearchRequest : PagedSortRequest
{
  public Guid? PatientId { get; set; }

  public Guid? MedicationId { get; set; }

  public bool? IsAcknowledged { get; set; }

  public bool? IsActive { get; set; }

  public DateOnly? DueFromDate { get; set; }

  public DateOnly? DueToDate { get; set; }
}
