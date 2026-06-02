using MediAlert.Constants;
using MediAlert.Data;
using MediAlert.DTOs.Appointments;
using MediAlert.Models;
using MediAlert.Services.Appointments.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace MediAlert.Services.Appointments;

public sealed class AppointmentService : IAppointmentService
{
    private readonly ApplicationDbContext _db;

    public AppointmentService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<AppointmentServiceResult<AppointmentResponse>> BookAppointmentAsync(Guid patientId, CreateAppointmentRequest request, CancellationToken cancellationToken = default)
    {
        var appointment = new Consultation
        {
            ConsultationId = Guid.NewGuid(),
            PatientId = patientId,
            DoctorId = request.DoctorId,
            ScheduledDateTime = request.ScheduledDateTime,
            Type = request.AppointmentType,
            Status = AppointmentStatuses.Scheduled,
            CreatedAt = DateTime.UtcNow
        };
        
        await _db.Consultations.AddAsync(appointment, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        
        return AppointmentServiceResult<AppointmentResponse>.Success(new AppointmentResponse { AppointmentId = appointment.ConsultationId, Status = appointment.Status });
    }

    public async Task<AppointmentServiceResult<AppointmentResponse>> RescheduleAppointmentAsync(Guid appointmentId, Guid patientId, DateTime newTime, CancellationToken cancellationToken = default)
    {
        var appointment = await _db.Consultations.FirstOrDefaultAsync(c => c.ConsultationId == appointmentId && c.PatientId == patientId, cancellationToken);
        if (appointment == null) return AppointmentServiceResult<AppointmentResponse>.Failure(AppointmentErrorCodes.AppointmentNotFound, "Appointment not found.", StatusCodes.Status404NotFound);
        
        appointment.ScheduledDateTime = newTime;
        appointment.Status = AppointmentStatuses.Rescheduled;
        await _db.SaveChangesAsync(cancellationToken);
        
        return AppointmentServiceResult<AppointmentResponse>.Success(new AppointmentResponse { AppointmentId = appointment.ConsultationId, Status = appointment.Status });
    }

    public async Task<AppointmentServiceResult<AppointmentResponse>> CancelAppointmentAsync(Guid appointmentId, Guid patientId, CancellationToken cancellationToken = default)
    {
        var appointment = await _db.Consultations.FirstOrDefaultAsync(c => c.ConsultationId == appointmentId && c.PatientId == patientId, cancellationToken);
        if (appointment == null) return AppointmentServiceResult<AppointmentResponse>.Failure(AppointmentErrorCodes.AppointmentNotFound, "Appointment not found.", StatusCodes.Status404NotFound);
        
        appointment.Status = AppointmentStatuses.Cancelled;
        await _db.SaveChangesAsync(cancellationToken);
        
        return AppointmentServiceResult<AppointmentResponse>.Success(new AppointmentResponse { AppointmentId = appointment.ConsultationId, Status = appointment.Status });
    }

    public Task<AppointmentServiceResult<bool>> GenerateRefillRemindersAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        // Placeholder for refill reminder generation
        return Task.FromResult(AppointmentServiceResult<bool>.Success(true));
    }

    public Task<AppointmentServiceResult<bool>> AcknowledgeRefillReminderAsync(Guid reminderId, Guid patientId, CancellationToken cancellationToken = default)
    {
        // Placeholder for acknowledging refill reminder
        return Task.FromResult(AppointmentServiceResult<bool>.Success(true));
    }

    public async Task<AppointmentServiceResult<List<AppointmentSummaryResponse>>> GetPatientAppointmentsAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        var consultations = await _db.Consultations
            .AsNoTracking()
            .Where(c => c.PatientId == patientId)
            .OrderByDescending(c => c.ScheduledDateTime)
            .Select(c => new AppointmentSummaryResponse
            {
                AppointmentId = c.ConsultationId,
                PatientId = c.PatientId,
                DoctorId = c.DoctorId,
                AppointmentType = c.Type,
                ScheduledDateTime = c.ScheduledDateTime,
                Status = c.Status,
                Location = c.ZoomMeetingUrl
            })
            .ToListAsync(cancellationToken);

        return AppointmentServiceResult<List<AppointmentSummaryResponse>>.Success(consultations);
    }
}
