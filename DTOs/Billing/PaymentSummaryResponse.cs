namespace MediAlert.DTOs.Billing;

public sealed class PaymentSummaryResponse
{
  public Guid PaymentId { get; set; }

  public decimal Amount { get; set; }

  public string Currency { get; set; } = "USD";

  public string PaymentType { get; set; } = string.Empty;

  public string Status { get; set; } = string.Empty;

  public DateTime CreatedAt { get; set; }
}
