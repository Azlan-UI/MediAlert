using System.ComponentModel.DataAnnotations;
using MediAlert.DTOs.Common;

namespace MediAlert.DTOs.Billing;

public sealed class SubscriptionSearchRequest : PagedSortRequest
{
  public string? UserId { get; set; }

  [StringLength(30, ErrorMessage = "Tier must not exceed 30 characters.")]
  public string? Tier { get; set; }

  [StringLength(30, ErrorMessage = "Status must not exceed 30 characters.")]
  public string? Status { get; set; }
}
