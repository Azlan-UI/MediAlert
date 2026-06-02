using MediAlert.Models;

namespace MediAlert.Repositories.Billing.Interfaces;

public interface IBillingRepository
{
    Task<Subscription?> GetSubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken = default);
    Task<Subscription?> GetActiveSubscriptionForPatientAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<Subscription?> GetSubscriptionByStripeIdAsync(string stripeSubscriptionId, CancellationToken cancellationToken = default);
    Task AddSubscriptionAsync(Subscription subscription, CancellationToken cancellationToken = default);
    
    Task<List<Invoice>> GetInvoicesForSubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken = default);
    Task AddInvoiceAsync(Invoice invoice, CancellationToken cancellationToken = default);
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
