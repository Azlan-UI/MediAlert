using System.ComponentModel.DataAnnotations;
using MediAlert.DTOs.Common;

namespace MediAlert.DTOs.Appointments;

public sealed class AppointmentSearchRequest : PagedSortRequest
{
  public Guid? PatientId { get; set; }

  public Guid? DoctorId { get; set; }

  [StringLength(30, ErrorMessage = "Appointment type must not exceed 30 characters.")]
  public string? AppointmentType { get; set; }

  [StringLength(20, ErrorMessage = "Status must not exceed 20 characters.")]
  public string? Status { get; set; }

  public DateTime? FromDateTime { get; set; }

  public DateTime? ToDateTime { get; set; }
}
