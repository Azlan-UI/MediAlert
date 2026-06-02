using System.ComponentModel.DataAnnotations;
using MediAlert.DTOs.Common;

namespace MediAlert.DTOs.Billing;

public sealed class InvoiceSearchRequest : PagedSortRequest
{
  public Guid? SubscriptionId { get; set; }

  [StringLength(20, ErrorMessage = "Status must not exceed 20 characters.")]
  public string? Status { get; set; }

  public DateOnly? DueFromDate { get; set; }

  public DateOnly? DueToDate { get; set; }

  public bool? IsOverdue { get; set; }
}
