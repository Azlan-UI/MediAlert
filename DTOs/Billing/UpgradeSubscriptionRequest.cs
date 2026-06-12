using System.ComponentModel.DataAnnotations;

namespace MediAlert.DTOs.Billing;

public sealed class UpgradeSubscriptionRequest
{
  [Required(ErrorMessage = "New tier is required.")]
  [StringLength(30, ErrorMessage = "Tier must not exceed 30 characters.")]
  public string NewTier { get; set; } = string.Empty;
}
