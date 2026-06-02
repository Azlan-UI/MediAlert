namespace MediAlert.DTOs.Billing;

public sealed class SubscriptionSummaryResponse
{
  public Guid SubscriptionId { get; set; }

  public string Tier { get; set; } = string.Empty;

  public string Status { get; set; } = string.Empty;

  public DateTime StartDate { get; set; }

  public DateTime? RenewalDate { get; set; }
}
