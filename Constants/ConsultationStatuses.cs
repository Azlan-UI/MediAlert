namespace MediAlert.Constants;

/// <summary>
/// All valid consultation status values.
/// Maps to Consultation.Status column — enforced by DB check constraint.
/// </summary>
public static class ConsultationStatuses
{
    public const string Scheduled = "Scheduled";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";

    public static readonly IReadOnlyList<string> All = [Scheduled, Completed, Cancelled];
}
