namespace MediAlert.Constants;

/// <summary>
/// All valid subscription status values.
/// Maps to Subscription.Status column.
/// </summary>
public static class SubscriptionStatuses
{
    public const string Free      = "Free";
    public const string Active    = "Active";
    public const string Pending   = "Pending";
    public const string Cancelled = "Cancelled";
    public const string Expired   = "Expired";
    public const string PastDue   = "PastDue";

    public static readonly IReadOnlyList<string> All = [Free, Active, Pending, Cancelled, Expired, PastDue];

    public static bool IsValid(string status) =>
        All.Contains(status, StringComparer.OrdinalIgnoreCase);
}
