namespace MediAlert.Models;

public class Invoice
{
    public Guid InvoiceId { get; set; }
    public Guid SubscriptionId { get; set; }
    public string? StripeInvoiceId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string Status { get; set; } = "Pending"; // Pending, Paid, Overdue
    public DateTime DueDate { get; set; }
    public DateTime? PaidDate { get; set; }
    public string? HostedInvoiceUrl { get; set; }
    public string? InvoicePdfUrl { get; set; }
    public int AttemptCount { get; set; }
    public DateTime? NextPaymentAttempt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Subscription Subscription { get; set; } = null!;
}
