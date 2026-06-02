namespace MediAlert.Constants;

/// <summary>
/// All valid appointment/consultation booking status values.
/// </summary>
public static class AppointmentStatuses
{
    public const string Scheduled   = "Scheduled";
    public const string Rescheduled = "Rescheduled";
    public const string Cancelled   = "Cancelled";
    public const string Completed   = "Completed";

    public static readonly IReadOnlyList<string> All = [Scheduled, Rescheduled, Cancelled, Completed];
}
