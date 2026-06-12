namespace MediAlert.Constants;

public static class BillingErrorCodes
{
    public const string InvalidRequest           = "billing_invalid_request";
    public const string PatientNotFound          = "billing_patient_not_found";
    public const string SubscriptionNotFound     = "billing_subscription_not_found";
    public const string InvoiceNotFound          = "billing_invoice_not_found";
    public const string ActiveSubscriptionExists = "billing_active_subscription_exists";
    public const string AlreadyCancelled         = "billing_already_cancelled";
    public const string NoActiveSubscription     = "billing_no_active_subscription";
    public const string Unauthorized             = "billing_unauthorized";
    public const string StripeError              = "billing_stripe_error";
    public const string SaveFailed               = "billing_save_failed";
}
