using MediAlert.Extensions;
using MediAlert.Services.Billing.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace MediAlert.Services.Billing;

public sealed class PremiumAccessRequirement : IAuthorizationRequirement
{
}

public sealed class PremiumAccessHandler : AuthorizationHandler<PremiumAccessRequirement>
{
    private readonly IServiceScopeFactory _scopeFactory;

    public PremiumAccessHandler(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PremiumAccessRequirement requirement)
    {
        var userId = context.User.GetUserId();
        if (userId != Guid.Empty)
        {
            using var scope = _scopeFactory.CreateScope();
            var billingService = scope.ServiceProvider.GetRequiredService<IStripeBillingService>();
            
            if (await billingService.HasPremiumAccessAsync(userId))
            {
                context.Succeed(requirement);
            }
        }
    }
}
