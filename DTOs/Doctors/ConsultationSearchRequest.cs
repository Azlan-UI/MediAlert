using System.ComponentModel.DataAnnotations;
using MediAlert.DTOs.Common;

namespace MediAlert.DTOs.Doctors;

public sealed class ConsultationSearchRequest : PagedSortRequest
{
  public Guid? PatientId { get; set; }

  public Guid? DoctorId { get; set; }

  [StringLength(30, ErrorMessage = "Status must not exceed 30 characters.")]
  public string? Status { get; set; }

  public bool? IsFlagged { get; set; }

  public DateTime? FromScheduledTime { get; set; }

  public DateTime? ToScheduledTime { get; set; }
}
