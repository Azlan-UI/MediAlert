using System.ComponentModel.DataAnnotations;
using MediAlert.DTOs.Common;

namespace MediAlert.DTOs.Caregiver;

/// <summary>
/// Caregiver searches their approved linked patients.
/// </summary>
public sealed class LinkedPatientSearchRequest : PagedSortRequest
{
  [StringLength(256, ErrorMessage = "Search term must not exceed 256 characters.")]
  public string? SearchTerm { get; set; }
}
