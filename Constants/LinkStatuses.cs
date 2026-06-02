namespace MediAlert.Constants;

/// <summary>
/// All valid link-request status values.
/// Used by both CaregiverPatientLink and PatientDoctorLink.
/// </summary>
public static class LinkStatuses
{
    public const string Pending  = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string Revoked  = "Revoked";

    public static readonly IReadOnlyList<string> All = [Pending, Approved, Rejected, Revoked];
}
