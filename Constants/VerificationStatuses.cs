namespace MediAlert.Constants;

/// <summary>
/// All valid doctor verification status values.
/// Maps to Doctor.VerificationStatus column.
/// </summary>
public static class VerificationStatuses
{
    public const string Pending  = "Pending";
    public const string Verified = "Verified";
    public const string Rejected = "Rejected";

    public static readonly IReadOnlyList<string> All = [Pending, Verified, Rejected];
}
