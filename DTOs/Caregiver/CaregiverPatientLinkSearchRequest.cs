using System.ComponentModel.DataAnnotations;
using MediAlert.DTOs.Common;

namespace MediAlert.DTOs.Caregiver;

public sealed class CaregiverPatientLinkSearchRequest : PagedSortRequest
{
  public Guid? PatientId { get; set; }

  public Guid? CaregiverId { get; set; }

  [StringLength(20, ErrorMessage = "Status must not exceed 20 characters.")]
  public string? Status { get; set; }

  [StringLength(256, ErrorMessage = "Patient name search must not exceed 256 characters.")]
  public string? PatientName { get; set; }

  [EmailAddress(ErrorMessage = "Please provide a valid email address.")]
  [StringLength(256, ErrorMessage = "Caregiver email must not exceed 256 characters.")]
  public string? CaregiverEmail { get; set; }
}
