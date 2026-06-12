using System;

namespace MediAlert.DTOs.Caregiver;

public sealed class CaregiverAppointmentResponse
{
    public Guid AppointmentId { get; set; }
    public string DoctorFullName { get; set; } = string.Empty;
    public string DoctorSpecialization { get; set; } = string.Empty;
    public DateTime ScheduledDateTime { get; set; }
    public string AppointmentType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ZoomMeetingUrl { get; set; }
}
