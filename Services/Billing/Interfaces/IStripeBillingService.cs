using MediAlert.DTOs.Billing;

namespace MediAlert.Services.Billing.Interfaces;

public interface IStripeBillingService
{
    Task<BillingServiceResult<CheckoutSessionResponse>> CreateCheckoutSessionAsync(Guid patientId, CreateSubscriptionCheckoutRequest request, CancellationToken cancellationToken = default);
    Task<BillingServiceResult<bool>> HandleWebhookAsync(string payload, string signature, CancellationToken cancellationToken = default);
    Task<BillingServiceResult<SubscriptionResponse>> UpgradeSubscriptionAsync(Guid patientId, UpgradeSubscriptionRequest request, CancellationToken cancellationToken = default);
    Task<BillingServiceResult<SubscriptionResponse>> DowngradeSubscriptionAsync(Guid patientId, string newTier, CancellationToken cancellationToken = default);
    Task<BillingServiceResult<SubscriptionResponse>> DowngradeToFreeAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<BillingServiceResult<SubscriptionResponse>> ReactivateSubscriptionAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<BillingServiceResult<bool>> CancelSubscriptionAsync(Guid patientId, CancelSubscriptionRequest? request = null, CancellationToken cancellationToken = default);
    Task<BillingServiceResult<SubscriptionResponse>> GetSubscriptionAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<BillingServiceResult<SubscriptionDetailsResponse>> GetSubscriptionDetailsAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<BillingServiceResult<IReadOnlyList<InvoiceSummaryResponse>>> GetInvoicesAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<bool> HasPremiumAccessAsync(Guid patientId, CancellationToken cancellationToken = default);
}
