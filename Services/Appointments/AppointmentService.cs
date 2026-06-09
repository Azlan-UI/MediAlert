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

    public async Task<AppointmentServiceResult<bool>> GenerateRefillRemindersAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        // Simple logic: Scan active medications. If no refill reminder exists, create one.
        var activeMeds = await _db.Medications
            .Where(m => m.PatientId == patientId && m.IsActive)
            .ToListAsync(cancellationToken);

        var existingReminders = await _db.RefillReminders
            .Where(r => r.PatientId == patientId && r.Status == "Pending")
            .Select(r => r.MedicationId)
            .ToListAsync(cancellationToken);

        var newReminders = new List<RefillReminder>();
        foreach (var med in activeMeds)
        {
            if (!existingReminders.Contains(med.MedicationId))
            {
                newReminders.Add(new RefillReminder
                {
                    ReminderId = Guid.NewGuid(),
                    PatientId = patientId,
                    MedicationId = med.MedicationId,
                    ReminderDate = DateTime.UtcNow.AddDays(7), // Remind in 7 days
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        if (newReminders.Any())
        {
            await _db.RefillReminders.AddRangeAsync(newReminders, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        return AppointmentServiceResult<bool>.Success(true);
    }

    public async Task<AppointmentServiceResult<bool>> AcknowledgeRefillReminderAsync(Guid reminderId, Guid patientId, CancellationToken cancellationToken = default)
    {
        var reminder = await _db.RefillReminders.FirstOrDefaultAsync(r => r.ReminderId == reminderId && r.PatientId == patientId, cancellationToken);
        if (reminder == null) return AppointmentServiceResult<bool>.Failure("RefillError", "Reminder not found.", StatusCodes.Status404NotFound);

        reminder.Status = "Acknowledged";
        await _db.SaveChangesAsync(cancellationToken);
        return AppointmentServiceResult<bool>.Success(true);
    }

    public async Task<AppointmentServiceResult<List<RefillReminderDto>>> GetPatientRefillsAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        var refills = await _db.RefillReminders
            .Include(r => r.Medication)
            .Where(r => r.PatientId == patientId)
            .OrderByDescending(r => r.ReminderDate)
            .Select(r => new RefillReminderDto
            {
                ReminderId = r.ReminderId,
                MedicationName = r.Medication.DrugName,
                ReminderDate = r.ReminderDate,
                Status = r.Status
            })
            .ToListAsync(cancellationToken);

        return AppointmentServiceResult<List<RefillReminderDto>>.Success(refills);
    }

    public async Task<AppointmentServiceResult<bool>> SaveConsultationNoteAsync(Guid appointmentId, string notes, CancellationToken cancellationToken = default)
    {
        var existingNote = await _db.ConsultationNotes.FirstOrDefaultAsync(n => n.ConsultationId == appointmentId, cancellationToken);
        
        if (existingNote != null)
        {
            existingNote.ClinicalNotes = notes;
        }
        else
        {
            await _db.ConsultationNotes.AddAsync(new ConsultationNote
            {
                ConsultationNoteId = Guid.NewGuid(),
                ConsultationId = appointmentId,
                ClinicalNotes = notes,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return AppointmentServiceResult<bool>.Success(true);
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
