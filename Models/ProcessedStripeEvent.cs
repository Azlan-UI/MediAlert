namespace MediAlert.Models;

public class ProcessedStripeEvent
{
    public string EventId { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
