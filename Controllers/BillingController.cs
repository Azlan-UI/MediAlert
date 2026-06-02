using MediAlert.Constants;
using MediAlert.DTOs.Billing;
using MediAlert.Services.Billing;
using MediAlert.Services.Billing.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediAlert.Controllers;

[ApiController]
[Authorize(Roles = UserRoles.Patient)]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class BillingController : ControllerBase
{
    private readonly IStripeBillingService _billingService;
    private readonly ILogger<BillingController> _logger;

    public BillingController(IStripeBillingService billingService, ILogger<BillingController> logger)
    {
        _billingService = billingService;
        _logger = logger;
    }

    [HttpPost("checkout")]
    public async Task<IActionResult> CreateCheckout([FromBody] CreateSubscriptionCheckoutRequest request, CancellationToken cancellationToken)
    {
        var patientId = GetUserId();
        var result = await _billingService.CreateCheckoutSessionAsync(patientId, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPut("upgrade")]
    public async Task<IActionResult> UpgradeSubscription([FromBody] UpgradeSubscriptionRequest request, CancellationToken cancellationToken)
    {
        var patientId = GetUserId();
        var result = await _billingService.UpgradeSubscriptionAsync(patientId, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPut("downgrade")]
    public async Task<IActionResult> DowngradeSubscription([FromBody] string newTier, CancellationToken cancellationToken)
    {
        var patientId = GetUserId();
        var result = await _billingService.DowngradeSubscriptionAsync(patientId, newTier, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("downgrade-free")]
    public async Task<IActionResult> DowngradeToFree(CancellationToken cancellationToken)
    {
        var patientId = GetUserId();
        var result = await _billingService.DowngradeToFreeAsync(patientId, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("reactivate")]
    public async Task<IActionResult> ReactivateSubscription(CancellationToken cancellationToken)
    {
        var patientId = GetUserId();
        var result = await _billingService.ReactivateSubscriptionAsync(patientId, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> StripeWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var signature = Request.Headers["Stripe-Signature"];
        
        var result = await _billingService.HandleWebhookAsync(json, signature!);
        if (result.Succeeded)
        {
            return Ok();
        }

        _logger.LogWarning(
            "Stripe webhook request failed. ErrorCode: {ErrorCode}. StatusCode: {StatusCode}",
            result.ErrorCode,
            result.StatusCode);

        return StatusCode(result.StatusCode, new
        {
            Message = "Stripe webhook processing failed.",
            result.ErrorCode
        });
    }

    [HttpPost("cancel")]
    public async Task<IActionResult> CancelSubscription([FromBody] CancelSubscriptionRequest? request, CancellationToken cancellationToken)
    {
        var patientId = GetUserId();
        var result = await _billingService.CancelSubscriptionAsync(patientId, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpDelete("cancel")]
    public async Task<IActionResult> CancelSubscription(CancellationToken cancellationToken)
    {
        var patientId = GetUserId();
        var result = await _billingService.CancelSubscriptionAsync(patientId, null, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("premium-access")]
    public async Task<IActionResult> HasPremiumAccess(CancellationToken cancellationToken)
    {
        var patientId = GetUserId();
        var hasAccess = await _billingService.HasPremiumAccessAsync(patientId, cancellationToken);
        return Ok(new { HasPremiumAccess = hasAccess });
    }

    [HttpGet("subscription")]
    public async Task<IActionResult> GetSubscription(CancellationToken cancellationToken)
    {
        var patientId = GetUserId();
        var result = await _billingService.GetSubscriptionAsync(patientId, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("subscription/details")]
    public async Task<IActionResult> GetSubscriptionDetails(CancellationToken cancellationToken)
    {
        var patientId = GetUserId();
        var result = await _billingService.GetSubscriptionDetailsAsync(patientId, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("invoices")]
    public async Task<IActionResult> GetInvoices(CancellationToken cancellationToken)
    {
        var patientId = GetUserId();
        var result = await _billingService.GetInvoicesAsync(patientId, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("payments")]
    public async Task<IActionResult> GetPaymentHistory(CancellationToken cancellationToken)
    {
        var patientId = GetUserId();
        var result = await _billingService.GetInvoicesAsync(patientId, cancellationToken);
        return ToActionResult(result);
    }

    private Guid GetUserId() => MediAlert.Extensions.ClaimsPrincipalExtensions.GetUserId(User);

    private IActionResult ToActionResult<T>(BillingServiceResult<T> result)
    {
        if (result.Succeeded) return StatusCode(result.StatusCode, result.Data);
        
        return StatusCode(result.StatusCode, new {
            Message = result.ErrorMessage ?? "Request failed.",
            ErrorCode = result.ErrorCode
        });
    }
}
