namespace MediAlert.Constants;

/// <summary>
/// Error code constants for the Caregiver Portal module.
/// Follows the same naming pattern as ComplianceErrorCodes.
/// </summary>
public static class CaregiverErrorCodes
{
    public const string InvalidRequest       = "caregiver_invalid_request";
    public const string CaregiverNotFound    = "caregiver_not_found";
    public const string PatientNotFound      = "caregiver_patient_not_found";
    public const string LinkNotFound         = "caregiver_link_not_found";
    public const string LinkAlreadyExists    = "caregiver_link_already_exists";
    public const string LinkNotApproved      = "caregiver_link_not_approved";
    public const string LinkAlreadyApproved  = "caregiver_link_already_approved";
    public const string Unauthorized         = "caregiver_unauthorized";
    public const string SaveFailed           = "caregiver_save_failed";
}
