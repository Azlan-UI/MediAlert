namespace MediAlert.Constants;

public static class AppointmentErrorCodes
{
    public const string InvalidRequest           = "appointment_invalid_request";
    public const string PatientNotFound          = "appointment_patient_not_found";
    public const string MedicationNotFound       = "appointment_medication_not_found";
    public const string AppointmentNotFound      = "appointment_not_found";
    public const string RefillReminderNotFound   = "appointment_refill_reminder_not_found";
    public const string PastDateTime             = "appointment_past_datetime";
    public const string AlreadyCancelled         = "appointment_already_cancelled";
    public const string AlreadyAcknowledged      = "appointment_already_acknowledged";
    public const string Unauthorized             = "appointment_unauthorized";
    public const string SaveFailed               = "appointment_save_failed";
}
