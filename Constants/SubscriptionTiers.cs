namespace MediAlert.Constants;

/// <summary>
/// All valid subscription tier values.
/// Maps to Subscription.Tier column.
/// </summary>
public static class SubscriptionTiers
{
    public const string Free    = "Free";
    public const string Premium = "Premium";

    public static readonly IReadOnlyList<string> All = [Free, Premium];

    public static bool IsValid(string tier) =>
        All.Contains(tier, StringComparer.OrdinalIgnoreCase);
}
