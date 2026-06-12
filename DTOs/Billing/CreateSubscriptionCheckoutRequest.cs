using System.ComponentModel.DataAnnotations;

namespace MediAlert.DTOs.Billing;

public sealed class CreateSubscriptionCheckoutRequest
{
  [Required(ErrorMessage = "Subscription tier is required.")]
  [StringLength(30, ErrorMessage = "Tier must not exceed 30 characters.")]
  public string Tier { get; set; } = string.Empty;

  [StringLength(500, ErrorMessage = "Success URL must not exceed 500 characters.")]
  public string? SuccessUrl { get; set; }

  [StringLength(500, ErrorMessage = "Cancel URL must not exceed 500 characters.")]
  public string? CancelUrl { get; set; }
}
