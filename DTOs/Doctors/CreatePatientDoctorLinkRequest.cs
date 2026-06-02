using System.ComponentModel.DataAnnotations;

namespace MediAlert.DTOs.Doctors;

public sealed class CreatePatientDoctorLinkRequest
{
  [Required(ErrorMessage = "Doctor ID is required.")]
  public Guid DoctorId { get; set; }
}
