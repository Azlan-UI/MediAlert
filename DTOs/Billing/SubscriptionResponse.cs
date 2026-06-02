namespace MediAlert.DTOs.Billing;

public sealed class SubscriptionResponse
{
  public Guid SubscriptionId { get; set; }

  public string UserId { get; set; } = string.Empty;

  public string Tier { get; set; } = string.Empty;

  public string Status { get; set; } = string.Empty;

  public string? StripeCustomerId { get; set; }

  public string? StripeSubscriptionId { get; set; }

  public DateTime StartDate { get; set; }

  public DateTime? RenewalDate { get; set; }

  public DateTime? CancelledAt { get; set; }

  public bool CancelAtPeriodEnd { get; set; }

  public DateTime CreatedAt { get; set; }

  public DateTime? UpdatedAt { get; set; }
}
