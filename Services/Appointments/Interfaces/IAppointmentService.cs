using MediAlert.DTOs.Appointments;

namespace MediAlert.Services.Appointments.Interfaces;

public interface IAppointmentService
{
    Task<AppointmentServiceResult<AppointmentResponse>> BookAppointmentAsync(Guid patientId, CreateAppointmentRequest request, CancellationToken cancellationToken = default);
    Task<AppointmentServiceResult<AppointmentResponse>> RescheduleAppointmentAsync(Guid appointmentId, Guid patientId, DateTime newTime, CancellationToken cancellationToken = default);
    Task<AppointmentServiceResult<AppointmentResponse>> CancelAppointmentAsync(Guid appointmentId, Guid patientId, CancellationToken cancellationToken = default);
    Task<AppointmentServiceResult<bool>> GenerateRefillRemindersAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<AppointmentServiceResult<bool>> AcknowledgeRefillReminderAsync(Guid reminderId, Guid patientId, CancellationToken cancellationToken = default);
    Task<AppointmentServiceResult<List<RefillReminderDto>>> GetPatientRefillsAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<AppointmentServiceResult<List<AppointmentSummaryResponse>>> GetPatientAppointmentsAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<AppointmentServiceResult<bool>> SaveConsultationNoteAsync(Guid appointmentId, string notes, CancellationToken cancellationToken = default);
}
