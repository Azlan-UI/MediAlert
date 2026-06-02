using MediAlert.Extensions;
using MediAlert.Services.Billing.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace MediAlert.Services.Billing;

public sealed class PremiumAccessRequirement : IAuthorizationRequirement
{
}

public sealed class PremiumAccessHandler : AuthorizationHandler<PremiumAccessRequirement>
{
    private readonly IStripeBillingService _billingService;

    public PremiumAccessHandler(IStripeBillingService billingService)
    {
        _billingService = billingService;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PremiumAccessRequirement requirement)
    {
        var userId = context.User.GetUserId();
        if (userId != Guid.Empty && await _billingService.HasPremiumAccessAsync(userId))
        {
            context.Succeed(requirement);
        }
    }
}
