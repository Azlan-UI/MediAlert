namespace MediAlert.DTOs.Billing;

public sealed class PaymentResponse
{
  public Guid PaymentId { get; set; }

  public string UserId { get; set; } = string.Empty;

  public Guid? SubscriptionId { get; set; }

  public string StripePaymentId { get; set; } = string.Empty;

  public decimal Amount { get; set; }

  public string Currency { get; set; } = "USD";

  public string PaymentType { get; set; } = string.Empty;

  public string Status { get; set; } = string.Empty;

  public DateTime CreatedAt { get; set; }
}
