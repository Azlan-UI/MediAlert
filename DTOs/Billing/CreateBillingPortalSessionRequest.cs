using System.ComponentModel.DataAnnotations;

namespace MediAlert.DTOs.Billing;

public sealed class CreateBillingPortalSessionRequest
{
  [Url(ErrorMessage = "Return URL must be a valid URL.")]
  [StringLength(500, ErrorMessage = "Return URL must not exceed 500 characters.")]
  public string? ReturnUrl { get; set; }
}
