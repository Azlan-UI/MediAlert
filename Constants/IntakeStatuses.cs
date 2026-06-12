namespace MediAlert.Constants;

public static class IntakeStatuses
{
    public const string Taken = "Taken";
    public const string Skipped = "Skipped";
    public const string Missed = "Missed";
    public const string Delayed = "Delayed";

    public static readonly IReadOnlyList<string> All =
        new[] { Taken, Skipped, Missed, Delayed };

    public static bool IsValid(string status) =>
        All.Contains(status, StringComparer.OrdinalIgnoreCase);
}
