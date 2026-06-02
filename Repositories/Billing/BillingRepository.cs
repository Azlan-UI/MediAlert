using MediAlert.Data;
using MediAlert.Models;
using MediAlert.Repositories.Billing.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MediAlert.Repositories.Billing;

public sealed class BillingRepository : IBillingRepository
{
    private readonly ApplicationDbContext _dbContext;

    public BillingRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Subscription?> GetSubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken = default) =>
        _dbContext.Subscriptions
            .AsTracking()
            .FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId, cancellationToken);

    public Task<Subscription?> GetActiveSubscriptionForPatientAsync(Guid patientId, CancellationToken cancellationToken = default) =>
        _dbContext.Subscriptions
            .AsTracking()
            .Where(s => s.PatientId == patientId && s.Status == "Active")
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<Subscription?> GetSubscriptionByStripeIdAsync(string stripeSubscriptionId, CancellationToken cancellationToken = default) =>
        _dbContext.Subscriptions
            .AsTracking()
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscriptionId, cancellationToken);

    public async Task AddSubscriptionAsync(Subscription subscription, CancellationToken cancellationToken = default)
    {
        await _dbContext.Subscriptions.AddAsync(subscription, cancellationToken);
    }

    public Task<List<Invoice>> GetInvoicesForSubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken = default) =>
        _dbContext.Invoices
            .AsNoTracking()
            .Where(i => i.SubscriptionId == subscriptionId)
            .OrderByDescending(i => i.DueDate)
            .ToListAsync(cancellationToken);

    public async Task AddInvoiceAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        await _dbContext.Invoices.AddAsync(invoice, cancellationToken);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
