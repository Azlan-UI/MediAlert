using System.ComponentModel.DataAnnotations;

namespace MediAlert.DTOs.Billing;

public sealed class CancelSubscriptionRequest
{
  [StringLength(500, ErrorMessage = "Cancellation reason must not exceed 500 characters.")]
  public string? Reason { get; set; }

  public bool CancelAtPeriodEnd { get; set; } = true;
}
