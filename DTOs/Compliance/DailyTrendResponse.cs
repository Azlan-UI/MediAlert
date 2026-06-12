namespace MediAlert.DTOs.Compliance;

public sealed class DailyTrendResponse
{
    public DateOnly Date { get; set; }
    public decimal CompliancePercentage { get; set; }
    public int TotalScheduled { get; set; }
    public int TakenDoses { get; set; }
    public int MissedDoses { get; set; }
}
