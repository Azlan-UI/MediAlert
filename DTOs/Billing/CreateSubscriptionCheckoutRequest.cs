using System.ComponentModel.DataAnnotations;

namespace MediAlert.DTOs.Billing;

public sealed class CreateSubscriptionCheckoutRequest
{
  [Required(ErrorMessage = "Subscription tier is required.")]
  [StringLength(30, ErrorMessage = "Tier must not exceed 30 characters.")]
  public string Tier { get; set; } = string.Empty;

  [Url(ErrorMessage = "Success URL must be a valid URL.")]
  [StringLength(500, ErrorMessage = "Success URL must not exceed 500 characters.")]
  public string? SuccessUrl { get; set; }

  [Url(ErrorMessage = "Cancel URL must be a valid URL.")]
  [StringLength(500, ErrorMessage = "Cancel URL must not exceed 500 characters.")]
  public string? CancelUrl { get; set; }
}
