using System.ComponentModel.DataAnnotations;
using MediAlert.DTOs.Common;

namespace MediAlert.DTOs.Billing;

public sealed class PaymentSearchRequest : PagedSortRequest
{
  public Guid? SubscriptionId { get; set; }

  [StringLength(20, ErrorMessage = "Status must not exceed 20 characters.")]
  public string? Status { get; set; }

  [StringLength(50, ErrorMessage = "Payment type must not exceed 50 characters.")]
  public string? PaymentType { get; set; }

  public DateTime? FromDate { get; set; }

  public DateTime? ToDate { get; set; }
}
