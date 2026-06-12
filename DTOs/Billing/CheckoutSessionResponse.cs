namespace MediAlert.DTOs.Billing;

public sealed class CheckoutSessionResponse
{
  public string SessionId { get; set; } = string.Empty;

  public string CheckoutUrl { get; set; } = string.Empty;

  public string Tier { get; set; } = string.Empty;
}
