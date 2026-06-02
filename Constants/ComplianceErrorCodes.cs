namespace MediAlert.Constants;

public static class ComplianceErrorCodes
{
    public const string InvalidRequest = "compliance_invalid_request";
    public const string PatientNotFound = "compliance_patient_not_found";
    public const string DoseScheduleNotFound = "compliance_dose_schedule_not_found";
    public const string InvalidStatus = "compliance_invalid_status";
    public const string NoScheduledDoses = "compliance_no_scheduled_doses";
    public const string SaveFailed = "compliance_save_failed";
}
