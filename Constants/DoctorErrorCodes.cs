namespace MediAlert.Constants;

public static class DoctorErrorCodes
{
    public const string InvalidRequest           = "doctor_invalid_request";
    public const string DoctorNotFound           = "doctor_not_found";
    public const string PatientNotFound          = "doctor_patient_not_found";
    public const string AvailabilityNotFound     = "doctor_availability_not_found";
    public const string OverlappingSlot          = "doctor_overlapping_slot";
    public const string InvalidTimeRange         = "doctor_invalid_time_range";
    public const string ConsultationNotFound     = "doctor_consultation_not_found";
    public const string ConsultationHasNotes     = "doctor_consultation_has_notes";
    public const string ConsultationNotScheduled = "doctor_consultation_not_scheduled";
    public const string AlreadyVerified          = "doctor_already_verified";
    public const string Unauthorized             = "doctor_unauthorized";
    public const string LinkNotFound             = "doctor_link_not_found";
    public const string LinkAlreadyExists        = "doctor_link_already_exists";
    public const string SaveFailed               = "doctor_save_failed";
}
