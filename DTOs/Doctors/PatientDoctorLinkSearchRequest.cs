using System.ComponentModel.DataAnnotations;
using MediAlert.DTOs.Common;

namespace MediAlert.DTOs.Doctors;

public sealed class PatientDoctorLinkSearchRequest : PagedSortRequest
{
  public Guid? PatientId { get; set; }

  public Guid? DoctorId { get; set; }

  [StringLength(20, ErrorMessage = "Status must not exceed 20 characters.")]
  public string? Status { get; set; }
}
