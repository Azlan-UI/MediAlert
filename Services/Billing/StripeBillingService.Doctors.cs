using MediAlert.Constants;
using MediAlert.DTOs.Billing;
using MediAlert.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
using LocalSubscription = MediAlert.Models.Subscription;
using SubStatus = MediAlert.Constants.SubscriptionStatuses;

namespace MediAlert.Services.Billing;

public sealed partial class StripeBillingService
{
    public async Task<BillingServiceResult<CheckoutSessionResponse>> CreateDoctorCheckoutSessionAsync(
        Guid doctorId,
        CreateSubscriptionCheckoutRequest request,
        CancellationToken cancellationToken = default)
    {
        var doctorIdText = doctorId.ToString();
        var doctor = await _db.Doctors.Include(d => d.User).FirstOrDefaultAsync(d => d.DoctorId == doctorId || d.UserId == doctorIdText, cancellationToken);
        if (doctor is null)
        {
            return Failure<CheckoutSessionResponse>(BillingErrorCodes.PatientNotFound, "Doctor profile was not found.", StatusCodes.Status404NotFound);
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

        var subscription = await _db.Subscriptions.Where(s => s.DoctorId == doctor.DoctorId).OrderByDescending(s => s.CreatedAt).FirstOrDefaultAsync(cancellationToken);
        if (subscription is null)
        {
            subscription = new LocalSubscription
            {
                DoctorId = doctor.DoctorId,
                Tier = SubscriptionTiers.Free,
                Status = SubStatus.Free
            };
            _db.Subscriptions.Add(subscription);
            await _db.SaveChangesAsync(cancellationToken);
        }

        var customerId = subscription.StripeCustomerId;
        if (string.IsNullOrWhiteSpace(customerId))
        {
            var customer = await new CustomerService().CreateAsync(
                new CustomerCreateOptions
                {
                    Email = doctor.User.Email,
                    Name = doctor.User.FullName,
                    Metadata = new Dictionary<string, string>
                    {
                        [MetadataDoctorId] = doctor.DoctorId.ToString(),
                        ["userId"] = doctor.UserId
                    }
                },
                cancellationToken: cancellationToken);
            customerId = customer.Id;
            subscription.StripeCustomerId = customerId;
            subscription.UpdatedAt = DateTime.UtcNow;
            LogAudit(null, doctor.DoctorId, "StripeCustomerCreated", $"Created Stripe customer {customer.Id}.");
            await _db.SaveChangesAsync(cancellationToken);
        }

        var options = new SessionCreateOptions
        {
            Customer = customerId,
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    Price = priceId,
                    Quantity = 1
                }
            },
            Mode = "subscription",
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            ClientReferenceId = doctor.DoctorId.ToString(),
            SubscriptionData = new SessionSubscriptionDataOptions
            {
                Metadata = new Dictionary<string, string>
                {
                    [MetadataDoctorId] = doctor.DoctorId.ToString(),
                    [MetadataTier] = request.Tier
                }
            }
        };

        try
        {
            var session = await new SessionService().CreateAsync(options, cancellationToken: cancellationToken);
            LogAudit(null, doctor.DoctorId, "SubscriptionCreated", $"Started checkout session {session.Id} for {request.Tier}.");
            await _db.SaveChangesAsync(cancellationToken);

            return BillingServiceResult<CheckoutSessionResponse>.Success(new CheckoutSessionResponse
            {
                SessionId = session.Id,
                CheckoutUrl = session.Url,
                Tier = request.Tier
            });
        }
        catch (StripeException e)
        {
            return Failure<CheckoutSessionResponse>(BillingErrorCodes.StripeError, e.Message);
        }
    }

    public async Task<BillingServiceResult<bool>> VerifyDoctorCheckoutSessionAsync(Guid doctorId, string sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            StripeConfiguration.ApiKey = _settings.SecretKey;
            var sessionService = new SessionService();
            var session = await sessionService.GetAsync(sessionId, cancellationToken: cancellationToken);

            if (session.PaymentStatus == "paid" || session.Status == "complete")
            {
                var doctorIdText = doctorId.ToString();
                var doctor = await _db.Doctors.FirstOrDefaultAsync(d => d.DoctorId == doctorId || d.UserId == doctorIdText, cancellationToken);
                if (doctor == null) return BillingServiceResult<bool>.Success(false);

                var subscription = await _db.Subscriptions
                    .Where(s => s.DoctorId == doctor.DoctorId)
                    .OrderByDescending(s => s.CreatedAt)
                    .FirstOrDefaultAsync(cancellationToken);
                if (subscription != null && (subscription.Status != SubStatus.Active || subscription.Tier != SubscriptionTiers.Premium))
                {
                    subscription.Status = SubStatus.Active;
                    subscription.Tier = SubscriptionTiers.Premium;
                    subscription.StripeCustomerId = session.CustomerId;
                    subscription.UpdatedAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync(cancellationToken);
                    return BillingServiceResult<bool>.Success(true);
                }
            }
            return BillingServiceResult<bool>.Success(false);
        }
        catch (StripeException e)
        {
            return Failure<bool>(BillingErrorCodes.StripeError, e.Message);
        }
    }

    public async Task<bool> HasDoctorPremiumAccessAsync(Guid doctorId, CancellationToken cancellationToken = default)
    {
        var doctorIdText = doctorId.ToString();
        var doctor = await _db.Doctors.FirstOrDefaultAsync(d => d.DoctorId == doctorId || d.UserId == doctorIdText, cancellationToken);
        if (doctor is null)
        {
            return false;
        }

        var subscription = await _db.Subscriptions
            .Where(s => s.DoctorId == doctor.DoctorId)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (subscription is null)
        {
            return false;
        }

        await ExpireIfNeededAsync(subscription, cancellationToken);
        return HasActivePremiumState(subscription);
    }
}
