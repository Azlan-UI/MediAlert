namespace MediAlert.DTOs.Billing;

public sealed class InvoiceResponse
{
  public Guid InvoiceId { get; set; }

  public Guid SubscriptionId { get; set; }

  public string? StripeInvoiceId { get; set; }

  public decimal Amount { get; set; }

  public string Currency { get; set; } = "USD";

  public string Status { get; set; } = string.Empty;

  public DateOnly DueDate { get; set; }

  public DateOnly? PaidDate { get; set; }

  public DateTime CreatedAt { get; set; }

  public DateTime? UpdatedAt { get; set; }
}
