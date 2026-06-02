using System.ComponentModel.DataAnnotations;
using MediAlert.DTOs.Common;

namespace MediAlert.DTOs.Doctors;

public sealed class DoctorSearchRequest : PagedSortRequest
{
  [StringLength(200, ErrorMessage = "Specialization must not exceed 200 characters.")]
  public string? Specialization { get; set; }

  [StringLength(256, ErrorMessage = "Search term must not exceed 256 characters.")]
  public string? SearchTerm { get; set; }

  public bool? IsVerified { get; set; }

  [StringLength(20, ErrorMessage = "Verification status must not exceed 20 characters.")]
  public string? VerificationStatus { get; set; }
}
