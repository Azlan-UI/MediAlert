using MediAlert.Configuration;
using MediAlert.Constants;
using MediAlert.Data;
using MediAlert.DTOs.Billing;
using MediAlert.Models;
using MediAlert.Services.Billing.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using LocalInvoice = MediAlert.Models.Invoice;
using LocalSubscription = MediAlert.Models.Subscription;
using SubStatus = MediAlert.Constants.SubscriptionStatuses;

namespace MediAlert.Services.Billing;

public sealed partial class StripeBillingService : IStripeBillingService
{
    private const string MetadataPatientId = "patientId";
    private const string MetadataDoctorId = "doctorId";
    private const string MetadataTier = "tier";

    private readonly ApplicationDbContext _db;
    private readonly StripeSettings _settings;
    private readonly ILogger<StripeBillingService> _logger;

    public StripeBillingService(
        ApplicationDbContext db,
        IOptions<StripeSettings> settings,
        ILogger<StripeBillingService> logger)
    {
        _db = db;
        _settings = settings.Value;
        _logger = logger;
        StripeConfiguration.ApiKey = _settings.SecretKey;
    }

    public async Task<BillingServiceResult<CheckoutSessionResponse>> CreateCheckoutSessionAsync(
        Guid patientId,
        CreateSubscriptionCheckoutRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await GetBillingUserAsync(patientId, cancellationToken);
        if (user is null)
        {
            return Failure<CheckoutSessionResponse>(BillingErrorCodes.PatientNotFound, "Patient profile was not found.", StatusCodes.Status404NotFound);
        }

        if (request is null || !IsPaidTier(request.Tier))
        {
            return Failure<CheckoutSessionResponse>(BillingErrorCodes.InvalidRequest, "A paid subscription tier is required.");
        }

        if (!TryGetConfiguredPriceId(request.Tier, out var priceId))
        {
            return Failure<CheckoutSessionResponse>(BillingErrorCodes.InvalidRequest, $"Stripe price ID is not configured for the {request.Tier} tier.", StatusCodes.Status500InternalServerError);
        }

        var successUrl = string.IsNullOrWhiteSpace(request.SuccessUrl) ? _settings.SuccessUrl : request.SuccessUrl;
        var cancelUrl = string.IsNullOrWhiteSpace(request.CancelUrl) ? _settings.CancelUrl : request.CancelUrl;
        if (!HasStripeCheckoutConfiguration(successUrl, cancelUrl))
        {
            return Failure<CheckoutSessionResponse>(BillingErrorCodes.InvalidRequest, "Stripe checkout configuration is incomplete.", StatusCodes.Status500InternalServerError);
        }

        var current = await EnsureLocalSubscriptionAsync(user, cancellationToken);
        if (HasActivePremiumState(current) && current.Tier.Equals(request.Tier, StringComparison.OrdinalIgnoreCase))
        {
            return Failure<CheckoutSessionResponse>(BillingErrorCodes.ActiveSubscriptionExists, "An active premium subscription already exists.", StatusCodes.Status409Conflict);
        }

        StripeConfiguration.ApiKey = _settings.SecretKey;
        var stripeCustomerId = await EnsureStripeCustomerAsync(user, current, cancellationToken);

        current.Tier = request.Tier;
        current.Status = SubStatus.Pending;
        current.StripeCustomerId = stripeCustomerId;
        current.StripePriceId = priceId;
        current.UpdatedAt = DateTime.UtcNow;

        var options = new SessionCreateOptions
        {
            Mode = "subscription",
            Customer = stripeCustomerId,
            ClientReferenceId = user.PrimaryId.ToString(),
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            AllowPromotionCodes = true,
            BillingAddressCollection = "auto",
            LineItems =
            [
                new()
                {
                    Price = priceId,
                    Quantity = 1
                }
            ],
            Metadata = BuildStripeMetadata(user.PrimaryId, request.Tier),
            SubscriptionData = new SessionSubscriptionDataOptions
            {
                Metadata = BuildStripeMetadata(user.PrimaryId, request.Tier)
            }
        };

        try
        {
            var session = await new SessionService().CreateAsync(options, cancellationToken: cancellationToken);
            LogAudit(user.PrimaryId, null, "SubscriptionCreated", $"Stripe Checkout session {session.Id} completed successfully.");
            await _db.SaveChangesAsync(cancellationToken);

            return BillingServiceResult<CheckoutSessionResponse>.Success(new CheckoutSessionResponse
            {
                SessionId = session.Id,
                CheckoutUrl = session.Url,
                Tier = request.Tier
            }, StatusCodes.Status201Created);
        }
        catch (StripeException e)
        {
            return Failure<CheckoutSessionResponse>(BillingErrorCodes.StripeError, e.Message);
        }
    }

    public async Task<BillingServiceResult<bool>> HandleWebhookAsync(
        string payload,
        string signature,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.WebhookSecret) || string.IsNullOrWhiteSpace(signature))
        {
            _logger.LogWarning("Stripe webhook rejected because the webhook secret or signature header is missing.");
            return Failure<bool>(BillingErrorCodes.InvalidRequest, "Stripe webhook signature configuration is incomplete.");
        }

        try
        {
            StripeConfiguration.ApiKey = _settings.SecretKey;
            var stripeEvent = EventUtility.ConstructEvent(payload, signature, _settings.WebhookSecret);
            _logger.LogInformation("Processing Stripe webhook event {EventId} of type {EventType}.", stripeEvent.Id, stripeEvent.Type);

            if (await _db.ProcessedStripeEvents.AnyAsync(e => e.EventId == stripeEvent.Id, cancellationToken))
            {
                _logger.LogInformation("Stripe webhook event {EventId} was already processed.", stripeEvent.Id);
                return BillingServiceResult<bool>.Success(true);
            }

            switch (stripeEvent.Type)
            {
                case "checkout.session.completed":
                    await HandleCheckoutSessionCompletedAsync(stripeEvent, cancellationToken);
                    break;
                case "customer.subscription.created":
                case "customer.subscription.updated":
                    await HandleSubscriptionUpsertedAsync(stripeEvent, cancellationToken);
                    break;
                case "customer.subscription.deleted":
                    await HandleSubscriptionDeletedAsync(stripeEvent, cancellationToken);
                    break;
                case "invoice.payment_succeeded":
                    await HandleInvoiceAsync(stripeEvent, InvoiceStatuses.Paid, cancellationToken);
                    break;
                case "invoice.payment_failed":
                    await HandleInvoiceAsync(stripeEvent, InvoiceStatuses.Failed, cancellationToken);
                    break;
            }

            _db.ProcessedStripeEvents.Add(new ProcessedStripeEvent { EventId = stripeEvent.Id });
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Stripe webhook event {EventId} processed successfully.", stripeEvent.Id);
            return BillingServiceResult<bool>.Success(true);
        }
        catch (StripeException e)
        {
            _logger.LogWarning(e, "Stripe webhook signature validation or Stripe processing failed.");
            return Failure<bool>(BillingErrorCodes.StripeError, e.Message);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Stripe webhook processing failed while synchronizing local billing state.");
            return Failure<bool>(BillingErrorCodes.SaveFailed, e.Message, StatusCodes.Status500InternalServerError);
        }
    }

    public async Task<BillingServiceResult<SubscriptionResponse>> UpgradeSubscriptionAsync(
        Guid patientId,
        UpgradeSubscriptionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null || !IsPaidTier(request.NewTier))
        {
            return Failure<SubscriptionResponse>(BillingErrorCodes.InvalidRequest, "A paid subscription tier is required.");
        }

        var user = await GetBillingUserAsync(patientId, cancellationToken);
        if (user is null)
        {
            return Failure<SubscriptionResponse>(BillingErrorCodes.PatientNotFound, "Patient profile was not found.", StatusCodes.Status404NotFound);
        }

        if (!TryGetConfiguredPriceId(request.NewTier, out var priceId))
        {
            return Failure<SubscriptionResponse>(BillingErrorCodes.InvalidRequest, $"Stripe price ID is not configured for the {request.NewTier} tier.", StatusCodes.Status500InternalServerError);
        }

        var subscription = await EnsureLocalSubscriptionAsync(user, cancellationToken);
        if (string.IsNullOrWhiteSpace(subscription.StripeSubscriptionId))
        {
            return Failure<SubscriptionResponse>(BillingErrorCodes.NoActiveSubscription, "Start a Stripe Checkout session to upgrade from Free to Premium.", StatusCodes.Status409Conflict);
        }

        try
        {
            StripeConfiguration.ApiKey = _settings.SecretKey;
            var stripeSubscriptionService = new Stripe.SubscriptionService();
            var stripeSubscription = await stripeSubscriptionService.GetAsync(
                subscription.StripeSubscriptionId,
                new SubscriptionGetOptions { Expand = ["items"] },
                cancellationToken: cancellationToken);

            var itemId = stripeSubscription.Items?.Data?.FirstOrDefault()?.Id;
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return Failure<SubscriptionResponse>(BillingErrorCodes.StripeError, "Stripe subscription item was not found.", StatusCodes.Status502BadGateway);
            }

            var updated = await stripeSubscriptionService.UpdateAsync(
                subscription.StripeSubscriptionId,
                new SubscriptionUpdateOptions
                {
                    CancelAtPeriodEnd = false,
                    Items =
                    [
                        new SubscriptionItemOptions
                        {
                            Id = itemId,
                            Price = priceId
                        }
                    ],
                    Metadata = BuildStripeMetadata(user.PrimaryId, request.NewTier),
                    ProrationBehavior = "create_prorations"
                },
                cancellationToken: cancellationToken);

            ApplySubscriptionState(subscription, request.NewTier, MapStripeSubscriptionStatus(updated.Status), priceId, null);
            subscription.CancelAtPeriodEnd = false;
            subscription.CancelledAt = null;
            LogAudit(user.PrimaryId, null, "SubscriptionUpgraded", $"Subscription upgraded to {request.NewTier}.");
            await _db.SaveChangesAsync(cancellationToken);

            return BillingServiceResult<SubscriptionResponse>.Success(MapSubscription(subscription));
        }
        catch (StripeException e)
        {
            return Failure<SubscriptionResponse>(BillingErrorCodes.StripeError, e.Message);
        }
    }

    public Task<BillingServiceResult<SubscriptionResponse>> DowngradeSubscriptionAsync(
        Guid patientId,
        string newTier,
        CancellationToken cancellationToken = default)
    {
        return newTier.Equals(SubscriptionTiers.Free, StringComparison.OrdinalIgnoreCase)
            ? DowngradeToFreeAsync(patientId, cancellationToken)
            : UpgradeSubscriptionAsync(patientId, new UpgradeSubscriptionRequest { NewTier = newTier }, cancellationToken);
    }

    public async Task<BillingServiceResult<SubscriptionResponse>> DowngradeToFreeAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        var user = await GetBillingUserAsync(patientId, cancellationToken);
        if (user is null)
        {
            return Failure<SubscriptionResponse>(BillingErrorCodes.PatientNotFound, "Patient profile was not found.", StatusCodes.Status404NotFound);
        }

        var subscription = await GetLatestSubscriptionForUserAsync(user, cancellationToken);
        if (subscription is null || subscription.Tier == SubscriptionTiers.Free)
        {
            subscription = await EnsureLocalSubscriptionAsync(user, cancellationToken);
            subscription.Tier = SubscriptionTiers.Free;
            subscription.Status = SubStatus.Free;
            await _db.SaveChangesAsync(cancellationToken);
            return BillingServiceResult<SubscriptionResponse>.Success(MapSubscription(subscription));
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(subscription.StripeSubscriptionId))
            {
                StripeConfiguration.ApiKey = _settings.SecretKey;
                await new Stripe.SubscriptionService().CancelAsync(subscription.StripeSubscriptionId, cancellationToken: cancellationToken);
            }

            MarkFree(subscription, SubStatus.Free);
            LogAudit(user.PrimaryId, null, "SubscriptionDowngraded", "Subscription downgraded to Free tier.");
            await _db.SaveChangesAsync(cancellationToken);

            return BillingServiceResult<SubscriptionResponse>.Success(MapSubscription(subscription));
        }
        catch (StripeException e)
        {
            return Failure<SubscriptionResponse>(BillingErrorCodes.StripeError, e.Message);
        }
    }

    public async Task<BillingServiceResult<SubscriptionResponse>> ReactivateSubscriptionAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        var user = await GetBillingUserAsync(patientId, cancellationToken);
        if (user is null)
        {
            return Failure<SubscriptionResponse>(BillingErrorCodes.PatientNotFound, "Patient profile was not found.", StatusCodes.Status404NotFound);
        }

        var subscription = await GetLatestSubscriptionForUserAsync(user, cancellationToken);
        if (subscription is null || string.IsNullOrWhiteSpace(subscription.StripeSubscriptionId))
        {
            return Failure<SubscriptionResponse>(BillingErrorCodes.NoActiveSubscription, "No Stripe subscription is available to reactivate.", StatusCodes.Status404NotFound);
        }

        try
        {
            StripeConfiguration.ApiKey = _settings.SecretKey;
            var updated = await new Stripe.SubscriptionService().UpdateAsync(
                subscription.StripeSubscriptionId,
                new SubscriptionUpdateOptions
                {
                    CancelAtPeriodEnd = false,
                    Metadata = BuildStripeMetadata(user.PrimaryId, SubscriptionTiers.Premium)
                },
                cancellationToken: cancellationToken);

            ApplySubscriptionState(subscription, SubscriptionTiers.Premium, MapStripeSubscriptionStatus(updated.Status), subscription.StripePriceId, subscription.CurrentPeriodEnd);
            subscription.CancelAtPeriodEnd = false;
            subscription.CancelledAt = null;
            LogAudit(user.PrimaryId, null, "SubscriptionReactivated", $"Reactivated Stripe subscription {subscription.StripeSubscriptionId}.");
            await _db.SaveChangesAsync(cancellationToken);

            return BillingServiceResult<SubscriptionResponse>.Success(MapSubscription(subscription));
        }
        catch (StripeException e)
        {
            return Failure<SubscriptionResponse>(BillingErrorCodes.StripeError, e.Message);
        }
    }

    public async Task<BillingServiceResult<bool>> CancelSubscriptionAsync(
        Guid patientId,
        CancelSubscriptionRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var user = await GetBillingUserAsync(patientId, cancellationToken);
        if (user is null)
        {
            return Failure<bool>(BillingErrorCodes.PatientNotFound, "Patient profile was not found.", StatusCodes.Status404NotFound);
        }

        request ??= new CancelSubscriptionRequest();
        var subscription = await GetLatestSubscriptionForUserAsync(user, cancellationToken);
        if (subscription is null || subscription.Tier == SubscriptionTiers.Free)
        {
            return Failure<bool>(BillingErrorCodes.NoActiveSubscription, "No premium subscription found.", StatusCodes.Status404NotFound);
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(subscription.StripeSubscriptionId))
            {
                StripeConfiguration.ApiKey = _settings.SecretKey;
                var stripeSubscriptionService = new Stripe.SubscriptionService();

                if (request.CancelAtPeriodEnd)
                {
                    await stripeSubscriptionService.UpdateAsync(
                        subscription.StripeSubscriptionId,
                        new SubscriptionUpdateOptions
                        {
                            CancelAtPeriodEnd = true,
                            Metadata = BuildStripeMetadata(user.PrimaryId, subscription.Tier)
                        },
                        cancellationToken: cancellationToken);

                    subscription.CancelAtPeriodEnd = true;
                    subscription.Status = SubStatus.Cancelled;
                    subscription.CancelledAt = DateTime.UtcNow;
                    subscription.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    await stripeSubscriptionService.CancelAsync(subscription.StripeSubscriptionId, cancellationToken: cancellationToken);
                    MarkFree(subscription, SubStatus.Cancelled);
                }
            }
            else
            {
                MarkFree(subscription, SubStatus.Cancelled);
            }

            LogAudit(user.PrimaryId, null, request.CancelAtPeriodEnd ? "SubscriptionCancelScheduled" : "SubscriptionCancelled", request.Reason ?? "Cancellation requested.");
            await _db.SaveChangesAsync(cancellationToken);
            return BillingServiceResult<bool>.Success(true);
        }
        catch (StripeException e)
        {
            return Failure<bool>(BillingErrorCodes.StripeError, e.Message);
        }
    }

    public async Task<BillingServiceResult<SubscriptionResponse>> GetSubscriptionAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        var response = await GetSubscriptionDetailsAsResponseAsync(patientId, cancellationToken);
        return response;
    }

    public async Task<BillingServiceResult<SubscriptionDetailsResponse>> GetSubscriptionDetailsAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        var user = await GetBillingUserAsync(patientId, cancellationToken);
        if (user is null)
        {
            return Failure<SubscriptionDetailsResponse>(BillingErrorCodes.PatientNotFound, "Patient profile was not found.", StatusCodes.Status404NotFound);
        }

        var subscription = await EnsureLocalSubscriptionAsync(user, cancellationToken);
        await ExpireIfNeededAsync(subscription, cancellationToken);

        var invoices = await GetInvoiceSummariesAsync(subscription.SubscriptionId, cancellationToken);
        return BillingServiceResult<SubscriptionDetailsResponse>.Success(new SubscriptionDetailsResponse
        {
            SubscriptionId = subscription.SubscriptionId,
            UserId = user.UserId.ToString(),
            UserFullName = user.FullName,
            UserEmail = user.Email ?? string.Empty,
            Tier = subscription.Tier,
            Status = subscription.Status,
            StripeCustomerId = subscription.StripeCustomerId,
            StripeSubscriptionId = subscription.StripeSubscriptionId,
            StartDate = subscription.CreatedAt,
            RenewalDate = subscription.CurrentPeriodEnd,
            CancelledAt = subscription.CancelledAt,
            CancelAtPeriodEnd = subscription.CancelAtPeriodEnd,
            RecentInvoices = invoices.Take(5).ToList(),
            CreatedAt = subscription.CreatedAt,
            UpdatedAt = subscription.UpdatedAt
        });
    }

    public async Task<BillingServiceResult<IReadOnlyList<InvoiceSummaryResponse>>> GetInvoicesAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        var user = await GetBillingUserAsync(patientId, cancellationToken);
        if (user is null)
        {
            return Failure<IReadOnlyList<InvoiceSummaryResponse>>(BillingErrorCodes.PatientNotFound, "Patient profile was not found.", StatusCodes.Status404NotFound);
        }

        var subscription = await GetLatestSubscriptionForUserAsync(user, cancellationToken);
        if (subscription != null && !string.IsNullOrWhiteSpace(subscription.StripeCustomerId))
        {
            try
            {
                StripeConfiguration.ApiKey = _settings.SecretKey;
                var invoiceService = new Stripe.InvoiceService();
                var stripeInvoices = await invoiceService.ListAsync(new InvoiceListOptions
                {
                    Customer = subscription.StripeCustomerId,
                    Limit = 20
                }, cancellationToken: cancellationToken);

                var invoices = stripeInvoices.Data.Select(i => new InvoiceSummaryResponse
                {
                    InvoiceId = subscription.SubscriptionId,
                    SubscriptionId = subscription.SubscriptionId,
                    Amount = i.AmountDue / 100m,
                    Currency = i.Currency.ToUpperInvariant(),
                    Status = i.Status,
                    DueDate = DateOnly.FromDateTime(i.DueDate ?? i.Created),
                    PaidDate = i.Status == "paid" && i.StatusTransitions?.PaidAt != null 
                        ? DateOnly.FromDateTime(i.StatusTransitions.PaidAt.Value) 
                        : (i.Status == "paid" ? DateOnly.FromDateTime(i.Created) : null)
                }).ToList();

                return BillingServiceResult<IReadOnlyList<InvoiceSummaryResponse>>.Success(invoices);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch invoices from Stripe.");
            }
        }

        var subscriptionIds = await _db.Subscriptions
            .AsNoTracking()
            .Where(s => (user.PatientId != null && s.PatientId == user.PatientId) || (user.DoctorId != null && s.DoctorId == user.DoctorId))
            .Select(s => s.SubscriptionId)
            .ToListAsync(cancellationToken);

        var localInvoices = await _db.Invoices
            .AsNoTracking()
            .Where(i => subscriptionIds.Contains(i.SubscriptionId))
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => MapInvoiceSummary(i))
            .ToListAsync(cancellationToken);

        return BillingServiceResult<IReadOnlyList<InvoiceSummaryResponse>>.Success(localInvoices);
    }

    public async Task<bool> HasPremiumAccessAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        var user = await GetBillingUserAsync(patientId, cancellationToken);
        if (user is null)
        {
            return false;
        }

        var subscription = await GetLatestSubscriptionForUserAsync(user, cancellationToken);
        if (subscription is null)
        {
            return false;
        }

        await ExpireIfNeededAsync(subscription, cancellationToken);
        return HasActivePremiumState(subscription);
    }

    private async Task HandleCheckoutSessionCompletedAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        var raw = stripeEvent.Data.RawJsonElement;
        var subscriptionId = ExtractString(raw, "subscription");
        var customerId = ExtractString(raw, "customer");
        var clientReferenceId = ExtractString(raw, "client_reference_id");

        if (string.IsNullOrWhiteSpace(subscriptionId)
            || string.IsNullOrWhiteSpace(customerId)
            || string.IsNullOrWhiteSpace(clientReferenceId)
            || !Guid.TryParse(clientReferenceId, out var userId))
        {
            _logger.LogWarning(
                "Stripe checkout.session.completed event {EventId} could not be mapped. Customer: {CustomerId}. Subscription: {SubscriptionId}. UserId: {UserId}.",
                stripeEvent.Id,
                customerId,
                subscriptionId,
                clientReferenceId);
            return;
        }

        var tier = ExtractMetadataValue(raw, MetadataTier) ?? SubscriptionTiers.Premium;
        var isDoctor = ExtractMetadataValue(raw, MetadataDoctorId) != null;
        var patientId = isDoctor ? (Guid?)null : userId;
        var doctorId = isDoctor ? (Guid?)userId : null;

        var subscription = await _db.Subscriptions.Where(s => s.PatientId == patientId && s.DoctorId == doctorId).OrderByDescending(s => s.CreatedAt).FirstOrDefaultAsync(cancellationToken);
        if (subscription is null)
        {
            _db.Subscriptions.Add(new LocalSubscription
            {
                PatientId = patientId,
                DoctorId = doctorId,
                Tier = tier,
                Status = SubStatus.Active,
                StripeCustomerId = customerId,
                StripeSubscriptionId = subscriptionId,
                CurrentPeriodEnd = null
            });
        }
        else
        {
            subscription.StripeCustomerId = customerId;
            subscription.StripeSubscriptionId = subscriptionId;
            ApplySubscriptionState(subscription, tier, SubStatus.Active, subscription.StripePriceId, subscription.CurrentPeriodEnd);
            subscription.CancelAtPeriodEnd = false;
            subscription.CancelledAt = null;
        }

        LogAudit(patientId, doctorId, "SubscriptionCreated", $"Checkout completed. Stripe subscription {subscriptionId}.");
        _logger.LogInformation("Activated Premium subscription {StripeSubscriptionId} for user {UserId} from checkout completion.", subscriptionId, userId);
    }

    private async Task HandleSubscriptionUpsertedAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        var stripeSubscription = stripeEvent.Data.Object as Stripe.Subscription;
        var raw = stripeEvent.Data.RawJsonElement;
        var subscriptionId = stripeSubscription?.Id ?? ExtractString(raw, "id");
        var customerId = stripeSubscription?.CustomerId ?? ExtractString(raw, "customer");
        var (patientId, doctorId) = await ResolveUserFromStripeSubscriptionAsync(stripeSubscription, raw, cancellationToken);
        if ((patientId == null && doctorId == null) || string.IsNullOrWhiteSpace(subscriptionId))
        {
            _logger.LogWarning(
                "Stripe subscription event {EventId} could not be mapped to a local patient or doctor. Subscription: {SubscriptionId}.",
                stripeEvent.Id,
                subscriptionId);
            return;
        }

        var tier = GetTierFromMetadata(stripeSubscription?.Metadata) ?? ExtractMetadataValue(raw, MetadataTier) ?? await GetExistingTierAsync(subscriptionId, cancellationToken);
        var status = MapStripeSubscriptionStatus(stripeSubscription?.Status ?? ExtractString(raw, "status"));
        var currentPeriodEnd = ExtractUnixDateTime(raw, "current_period_end");
        var cancelAtPeriodEnd = ExtractBoolean(raw, "cancel_at_period_end") ?? false;
        var priceId = ExtractFirstItemPriceId(raw);

        await UpsertSubscriptionAsync(patientId, doctorId, subscriptionId, customerId, tier, status, priceId, currentPeriodEnd, cancelAtPeriodEnd, cancellationToken);
        LogAudit(patientId, doctorId, "SubscriptionUpdated", $"Stripe subscription {subscriptionId} changed to {status}.");
        _logger.LogInformation("Synchronized Stripe subscription {StripeSubscriptionId} for user (Patient: {PatientId}, Doctor: {DoctorId}) to status {Status}.", subscriptionId, patientId, doctorId, status);
    }

    private async Task HandleSubscriptionDeletedAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        var raw = stripeEvent.Data.RawJsonElement;
        var subscriptionId = ExtractString(raw, "id");
        if (string.IsNullOrWhiteSpace(subscriptionId))
        {
            _logger.LogWarning("Stripe subscription.deleted event {EventId} did not include a subscription id.", stripeEvent.Id);
            return;
        }

        var subscription = await _db.Subscriptions.FirstOrDefaultAsync(s => s.StripeSubscriptionId == subscriptionId, cancellationToken);
        if (subscription is null)
        {
            _logger.LogWarning("Stripe subscription.deleted event {EventId} referenced unknown subscription {StripeSubscriptionId}.", stripeEvent.Id, subscriptionId);
            return;
        }

        MarkFree(subscription, SubStatus.Expired);
        subscription.CurrentPeriodEnd = ExtractUnixDateTime(raw, "current_period_end") ?? DateTime.UtcNow;
        LogAudit(subscription.PatientId, subscription.DoctorId, "SubscriptionExpired", $"Stripe subscription {subscriptionId} ended.");
        _logger.LogInformation("Expired local subscription for patient {PatientId} after Stripe subscription {StripeSubscriptionId} was deleted.", subscription.PatientId, subscriptionId);
    }

    private async Task HandleInvoiceAsync(Event stripeEvent, string invoiceStatus, CancellationToken cancellationToken)
    {
        var raw = stripeEvent.Data.RawJsonElement;
        var subscriptionId = ExtractString(raw, "subscription");
        if (string.IsNullOrWhiteSpace(subscriptionId))
        {
            _logger.LogWarning("Stripe invoice event {EventId} did not include a subscription id.", stripeEvent.Id);
            return;
        }

        var subscription = await _db.Subscriptions.FirstOrDefaultAsync(s => s.StripeSubscriptionId == subscriptionId, cancellationToken);
        if (subscription is null)
        {
            _logger.LogWarning("Stripe invoice event {EventId} referenced unknown subscription {StripeSubscriptionId}.", stripeEvent.Id, subscriptionId);
            return;
        }

        await UpsertInvoiceAsync(subscription, raw, invoiceStatus, cancellationToken);
        subscription.Status = invoiceStatus == InvoiceStatuses.Failed ? SubStatus.PastDue : SubStatus.Active;
        subscription.UpdatedAt = DateTime.UtcNow;

        LogAudit(
            subscription.PatientId,
            subscription.DoctorId,
            invoiceStatus == InvoiceStatuses.Failed ? "InvoicePaymentFailed" : "InvoicePaid",
            $"Stripe invoice {ExtractString(raw, "id")} processed as {invoiceStatus}.");
        _logger.LogInformation(
            "Synchronized Stripe invoice {StripeInvoiceId} for subscription {StripeSubscriptionId}; local subscription status is {Status}.",
            ExtractString(raw, "id"),
            subscriptionId,
            subscription.Status);
    }

    private async Task UpsertSubscriptionAsync(
        Guid? patientId,
        Guid? doctorId,
        string stripeSubscriptionId,
        string? stripeCustomerId,
        string tier,
        string status,
        string? stripePriceId,
        DateTime? currentPeriodEnd,
        bool cancelAtPeriodEnd,
        CancellationToken cancellationToken)
    {
        var effectiveStatus = cancelAtPeriodEnd && status == SubStatus.Active
            ? SubStatus.Cancelled
            : status;

        var subscription = await _db.Subscriptions.FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscriptionId, cancellationToken);
        
        if (subscription is null && patientId.HasValue)
        {
            subscription = await GetLatestSubscriptionForUserAsync(new BillingUser { PatientId = patientId.Value }, cancellationToken);
        }
        else if (subscription is null && doctorId.HasValue)
        {
            subscription = await _db.Subscriptions.Where(s => s.DoctorId == doctorId.Value).OrderByDescending(s => s.CreatedAt).FirstOrDefaultAsync(cancellationToken);
        }

        if (subscription is null)
        {
            _db.Subscriptions.Add(new LocalSubscription
            {
                PatientId = patientId,
                StripeCustomerId = stripeCustomerId,
                StripeSubscriptionId = stripeSubscriptionId,
                StripePriceId = stripePriceId,
                Tier = tier,
                Status = effectiveStatus,
                CurrentPeriodEnd = currentPeriodEnd,
                CancelAtPeriodEnd = cancelAtPeriodEnd,
                CancelledAt = cancelAtPeriodEnd ? DateTime.UtcNow : null
            });
            return;
        }

        subscription.StripeCustomerId = stripeCustomerId ?? subscription.StripeCustomerId;
        subscription.StripeSubscriptionId = stripeSubscriptionId;
        subscription.StripePriceId = stripePriceId ?? subscription.StripePriceId;
        ApplySubscriptionState(subscription, tier, effectiveStatus, subscription.StripePriceId, currentPeriodEnd);
        subscription.CancelAtPeriodEnd = cancelAtPeriodEnd;
        subscription.CancelledAt = cancelAtPeriodEnd ? DateTime.UtcNow : subscription.CancelledAt;
    }

    private async Task UpsertInvoiceAsync(LocalSubscription subscription, System.Text.Json.JsonElement? raw, string invoiceStatus, CancellationToken cancellationToken)
    {
        var stripeInvoiceId = ExtractString(raw, "id");
        if (string.IsNullOrWhiteSpace(stripeInvoiceId))
        {
            return;
        }

        var invoice = await _db.Invoices.FirstOrDefaultAsync(i => i.StripeInvoiceId == stripeInvoiceId, cancellationToken);
        if (invoice is null)
        {
            invoice = new LocalInvoice
            {
                SubscriptionId = subscription.SubscriptionId,
                StripeInvoiceId = stripeInvoiceId
            };
            _db.Invoices.Add(invoice);
        }

        invoice.Amount = (ExtractLong(raw, invoiceStatus == InvoiceStatuses.Paid ? "amount_paid" : "amount_due") ?? 0) / 100m;
        invoice.Currency = (ExtractString(raw, "currency") ?? "USD").ToUpperInvariant();
        invoice.Status = invoiceStatus;
        invoice.DueDate = ExtractUnixDateTime(raw, "due_date") ?? DateTime.UtcNow;
        invoice.PaidDate = invoiceStatus == InvoiceStatuses.Paid ? DateTime.UtcNow : null;
        invoice.HostedInvoiceUrl = ExtractString(raw, "hosted_invoice_url");
        invoice.InvoicePdfUrl = ExtractString(raw, "invoice_pdf");
        invoice.AttemptCount = (int)(ExtractLong(raw, "attempt_count") ?? 0);
        invoice.NextPaymentAttempt = ExtractUnixDateTime(raw, "next_payment_attempt");
        invoice.UpdatedAt = DateTime.UtcNow;
    }

    

    

    private async Task<string> EnsureStripeCustomerAsync(BillingUser user, LocalSubscription subscription, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(subscription.StripeCustomerId))
        {
            return subscription.StripeCustomerId;
        }

        var customer = await new CustomerService().CreateAsync(
            new CustomerCreateOptions
            {
                Email = user.Email,
                Name = user.FullName,
                Metadata = new Dictionary<string, string>
                {
                    [MetadataPatientId] = user.PrimaryId.ToString(),
                    ["userId"] = user.UserId.ToString()
                }
            },
            cancellationToken: cancellationToken);

        subscription.StripeCustomerId = customer.Id;
        subscription.UpdatedAt = DateTime.UtcNow;
        LogAudit(user.PrimaryId, null, "StripeCustomerCreated", $"Created Stripe customer {customer.Id}.");
        return customer.Id;
    }

    

    private async Task<BillingServiceResult<SubscriptionResponse>> GetSubscriptionDetailsAsResponseAsync(Guid patientId, CancellationToken cancellationToken)
    {
        var user = await GetBillingUserAsync(patientId, cancellationToken);
        if (user is null)
        {
            return Failure<SubscriptionResponse>(BillingErrorCodes.PatientNotFound, "Patient profile was not found.", StatusCodes.Status404NotFound);
        }

        var subscription = await EnsureLocalSubscriptionAsync(user, cancellationToken);
        
        if (subscription.Status == SubStatus.Pending && !string.IsNullOrWhiteSpace(subscription.StripeCustomerId))
        {
            try
            {
                StripeConfiguration.ApiKey = _settings.SecretKey;
                var subService = new Stripe.SubscriptionService();
                var stripeSubs = await subService.ListAsync(new SubscriptionListOptions
                {
                    Customer = subscription.StripeCustomerId,
                    Status = "active",
                    Limit = 1
                }, cancellationToken: cancellationToken);

                var activeSub = stripeSubs.Data.FirstOrDefault();
                if (activeSub != null)
                {
                    subscription.StripeSubscriptionId = activeSub.Id;
                    subscription.Status = SubStatus.Active;
                    subscription.CurrentPeriodEnd = ExtractUnixDateTime(activeSub.RawJsonElement, "current_period_end") ?? DateTime.UtcNow;
                    await _db.SaveChangesAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to sync subscription from Stripe.");
            }
        }

        await ExpireIfNeededAsync(subscription, cancellationToken);
        return BillingServiceResult<SubscriptionResponse>.Success(MapSubscription(subscription));
    }

    private async Task ExpireIfNeededAsync(LocalSubscription subscription, CancellationToken cancellationToken)
    {
        if (subscription.Tier == SubscriptionTiers.Premium
            && subscription.CurrentPeriodEnd.HasValue
            && subscription.CurrentPeriodEnd.Value <= DateTime.UtcNow
            && (subscription.CancelAtPeriodEnd || subscription.Status == SubStatus.Cancelled))
        {
            MarkFree(subscription, SubStatus.Expired);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<(Guid? PatientId, Guid? DoctorId)> ResolveUserFromStripeSubscriptionAsync(
        Stripe.Subscription? stripeSubscription,
        System.Text.Json.JsonElement? raw,
        CancellationToken cancellationToken)
    {
        var patientIdText = stripeSubscription?.Metadata is not null
            && stripeSubscription.Metadata.TryGetValue(MetadataPatientId, out var typedPatientId)
                ? typedPatientId
                : ExtractMetadataValue(raw, MetadataPatientId);

        var doctorIdText = stripeSubscription?.Metadata is not null
            && stripeSubscription.Metadata.TryGetValue(MetadataDoctorId, out var typedDoctorId)
                ? typedDoctorId
                : ExtractMetadataValue(raw, MetadataDoctorId);

        if (Guid.TryParse(patientIdText, out var patientId))
        {
            return (patientId, null);
        }

        if (Guid.TryParse(doctorIdText, out var doctorId))
        {
            return (null, doctorId);
        }

        var subscriptionId = stripeSubscription?.Id ?? ExtractString(raw, "id");
        if (string.IsNullOrWhiteSpace(subscriptionId))
        {
            return (null, null);
        }

        var existing = await _db.Subscriptions
            .AsNoTracking()
            .Where(s => s.StripeSubscriptionId == subscriptionId)
            .Select(s => new { s.PatientId, s.DoctorId })
            .FirstOrDefaultAsync(cancellationToken);

        return (existing?.PatientId, existing?.DoctorId);
    }

    private async Task<string> GetExistingTierAsync(string stripeSubscriptionId, CancellationToken cancellationToken)
    {
        var tier = await _db.Subscriptions
            .AsNoTracking()
            .Where(s => s.StripeSubscriptionId == stripeSubscriptionId)
            .Select(s => s.Tier)
            .FirstOrDefaultAsync(cancellationToken);

        return string.IsNullOrWhiteSpace(tier) ? SubscriptionTiers.Premium : tier;
    }

    private async Task<List<InvoiceSummaryResponse>> GetInvoiceSummariesAsync(Guid subscriptionId, CancellationToken cancellationToken) =>
        await _db.Invoices
            .AsNoTracking()
            .Where(i => i.SubscriptionId == subscriptionId)
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => MapInvoiceSummary(i))
            .ToListAsync(cancellationToken);

    private bool TryGetConfiguredPriceId(string tier, out string priceId)
    {
        priceId = string.Empty;
        if (string.IsNullOrWhiteSpace(_settings.SecretKey))
        {
            return false;
        }

        if (tier.Equals(SubscriptionTiers.Premium, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(_settings.PriceId))
        {
            priceId = _settings.PriceId;
            return true;
        }

        return _settings.PriceIds.TryGetValue(tier, out priceId!)
            && !string.IsNullOrWhiteSpace(priceId);
    }

    private bool HasStripeCheckoutConfiguration(string? successUrl, string? cancelUrl) =>
        !string.IsNullOrWhiteSpace(_settings.SecretKey)
        && !string.IsNullOrWhiteSpace(successUrl)
        && !string.IsNullOrWhiteSpace(cancelUrl);

    private static bool HasActivePremiumState(LocalSubscription subscription)
    {
        var inCurrentPeriod = subscription.CurrentPeriodEnd is null || subscription.CurrentPeriodEnd > DateTime.UtcNow;
        return subscription.Tier == SubscriptionTiers.Premium
            && (subscription.Status == SubStatus.Active
                || (subscription.Status == SubStatus.Cancelled && subscription.CancelAtPeriodEnd))
            && inCurrentPeriod;
    }

    private static bool IsPaidTier(string tier) =>
        SubscriptionTiers.IsValid(tier)
        && !tier.Equals(SubscriptionTiers.Free, StringComparison.OrdinalIgnoreCase);

    private static void ApplySubscriptionState(LocalSubscription subscription, string tier, string status, string? priceId, DateTime? currentPeriodEnd)
    {
        subscription.Tier = tier;
        subscription.Status = status;
        subscription.StripePriceId = priceId ?? subscription.StripePriceId;
        subscription.CurrentPeriodEnd = currentPeriodEnd ?? subscription.CurrentPeriodEnd;
        subscription.UpdatedAt = DateTime.UtcNow;
        if (status == SubStatus.Active)
        {
            subscription.CancelledAt = null;
        }
    }

    private static void MarkFree(LocalSubscription subscription, string terminalStatus)
    {
        subscription.Tier = SubscriptionTiers.Free;
        subscription.Status = terminalStatus;
        subscription.CancelAtPeriodEnd = false;
        subscription.CancelledAt = terminalStatus == SubStatus.Free || terminalStatus == SubStatus.Active ? null : DateTime.UtcNow;
        subscription.UpdatedAt = DateTime.UtcNow;
    }

    private static Dictionary<string, string> BuildStripeMetadata(Guid patientId, string tier) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            [MetadataPatientId] = patientId.ToString(),
            [MetadataTier] = tier
        };

    private static string? GetTierFromMetadata(Dictionary<string, string>? metadata)
    {
        if (metadata is null || !metadata.TryGetValue(MetadataTier, out var tier))
        {
            return null;
        }

        return SubscriptionTiers.IsValid(tier) ? tier : null;
    }

    private static string MapStripeSubscriptionStatus(string? stripeStatus) =>
        stripeStatus switch
        {
            "active" or "trialing" => SubStatus.Active,
            "past_due" or "unpaid" or "incomplete" => SubStatus.PastDue,
            "incomplete_expired" or "canceled" => SubStatus.Expired,
            _ => SubStatus.Cancelled
        };

    private static SubscriptionResponse MapSubscription(LocalSubscription subscription) =>
        new()
        {
            SubscriptionId = subscription.SubscriptionId,
            UserId = subscription.PatientId?.ToString() ?? subscription.DoctorId?.ToString() ?? string.Empty,
            Tier = subscription.Tier,
            Status = subscription.Status,
            StripeCustomerId = subscription.StripeCustomerId,
            StripeSubscriptionId = subscription.StripeSubscriptionId,
            StartDate = subscription.CreatedAt,
            RenewalDate = subscription.CurrentPeriodEnd,
            CancelledAt = subscription.CancelledAt,
            CancelAtPeriodEnd = subscription.CancelAtPeriodEnd,
            CreatedAt = subscription.CreatedAt,
            UpdatedAt = subscription.UpdatedAt
        };

    private static InvoiceSummaryResponse MapInvoiceSummary(LocalInvoice invoice) =>
        new()
        {
            InvoiceId = invoice.InvoiceId,
            SubscriptionId = invoice.SubscriptionId,
            Amount = invoice.Amount,
            Currency = invoice.Currency,
            Status = invoice.Status,
            DueDate = DateOnly.FromDateTime(invoice.DueDate),
            PaidDate = invoice.PaidDate.HasValue ? DateOnly.FromDateTime(invoice.PaidDate.Value) : null
        };

    private static BillingServiceResult<T> Failure<T>(string code, string message, int statusCode = StatusCodes.Status400BadRequest) =>
        BillingServiceResult<T>.Failure(code, message, statusCode);

    private static string? ExtractString(System.Text.Json.JsonElement? element, string propertyName)
    {
        if (!element.HasValue || !element.Value.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind == System.Text.Json.JsonValueKind.String ? property.GetString() : property.ToString();
    }

    private static long? ExtractLong(System.Text.Json.JsonElement? element, string propertyName)
    {
        if (!element.HasValue || !element.Value.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.TryGetInt64(out var value) ? value : null;
    }

    private static bool? ExtractBoolean(System.Text.Json.JsonElement? element, string propertyName)
    {
        if (!element.HasValue || !element.Value.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind == System.Text.Json.JsonValueKind.True
            ? true
            : property.ValueKind == System.Text.Json.JsonValueKind.False
                ? false
                : null;
    }

    
    private class BillingUser
    {
        public Guid UserId { get; set; }
        public Guid? PatientId { get; set; }
        public Guid? DoctorId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public Guid PrimaryId => PatientId ?? DoctorId ?? Guid.Empty;
    }

    private async Task<BillingUser?> GetBillingUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var idText = userId.ToString();
        var patient = await _db.Patients.Include(p => p.User).FirstOrDefaultAsync(p => p.PatientId == userId || p.UserId == idText, cancellationToken);
        if (patient != null && patient.User != null) 
            return new BillingUser { UserId = userId, PatientId = patient.PatientId, Email = patient.User.Email, FullName = patient.User.FullName };
        
        var doctor = await _db.Doctors.Include(d => d.User).FirstOrDefaultAsync(d => d.DoctorId == userId || d.UserId == idText, cancellationToken);
        if (doctor != null && doctor.User != null) 
            return new BillingUser { UserId = userId, DoctorId = doctor.DoctorId, Email = doctor.User.Email, FullName = doctor.User.FullName };
        
        return null;
    }

    private async Task<LocalSubscription> EnsureLocalSubscriptionAsync(BillingUser user, CancellationToken cancellationToken)
    {
        var subscription = await _db.Subscriptions
            .Where(s => (user.PatientId != null && s.PatientId == user.PatientId) || (user.DoctorId != null && s.DoctorId == user.DoctorId))
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (subscription is null)
        {
            subscription = new LocalSubscription
            {
                PatientId = user.PatientId,
                DoctorId = user.DoctorId,
                Tier = SubscriptionTiers.Free,
                Status = SubStatus.Free
            };
            _db.Subscriptions.Add(subscription);
            await _db.SaveChangesAsync(cancellationToken);
        }

        return subscription;
    }

    private async Task<LocalSubscription?> GetLatestSubscriptionForUserAsync(BillingUser user, CancellationToken cancellationToken) =>
        await _db.Subscriptions
            .Where(s => (user.PatientId != null && s.PatientId == user.PatientId) || (user.DoctorId != null && s.DoctorId == user.DoctorId))
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    private static DateTime? ExtractUnixDateTime(System.Text.Json.JsonElement? element, string propertyName)
    {
        var unixSeconds = ExtractLong(element, propertyName);
        return unixSeconds.HasValue ? DateTimeOffset.FromUnixTimeSeconds(unixSeconds.Value).UtcDateTime : null;
    }

    private static string? ExtractMetadataValue(System.Text.Json.JsonElement? element, string key)
    {
        if (!element.HasValue
            || !element.Value.TryGetProperty("metadata", out var metadata)
            || !metadata.TryGetProperty(key, out var value))
        {
            return null;
        }

        return value.GetString();
    }

    private static string? ExtractFirstItemPriceId(System.Text.Json.JsonElement? element)
    {
        if (!element.HasValue
            || !element.Value.TryGetProperty("items", out var items)
            || !items.TryGetProperty("data", out var data)
            || data.ValueKind != System.Text.Json.JsonValueKind.Array)
        {
            return null;
        }

        var first = data.EnumerateArray().FirstOrDefault();
        return first.ValueKind == System.Text.Json.JsonValueKind.Object
            && first.TryGetProperty("price", out var price)
            && price.TryGetProperty("id", out var id)
                ? id.GetString()
                : null;
    }

    private void LogAudit(Guid? patientId, Guid? doctorId, string action, string details)
    {
        _db.BillingAuditLogs.Add(new BillingAuditLog
        {
            PatientId = patientId,
            DoctorId = doctorId,
            Action = action,
            Details = details
        });
    }
}

