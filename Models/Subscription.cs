namespace MediAlert.Models;

public class Subscription
{
    public Guid SubscriptionId { get; set; }
    public Guid PatientId { get; set; }
    public string Tier { get; set; } = "Free"; // Free, Premium
    public string Status { get; set; } = "Active"; // Active, Pending, Cancelled, Expired, PastDue
    public string? StripeCustomerId { get; set; }
    public string? StripeSubscriptionId { get; set; }
    public string? StripePriceId { get; set; }
    public bool CancelAtPeriodEnd { get; set; }
    public DateTime? CurrentPeriodEnd { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Patient Patient { get; set; } = null!;
    public ICollection<Invoice> Invoices { get; set; } = [];
}
