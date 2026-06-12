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
    private readonly MediAlert.Services.Doctors.Interfaces.IZoomApiService _zoomService;
    private readonly MediAlert.Services.Billing.Interfaces.IStripeBillingService _billingService;
    private readonly MediAlert.Services.Notifications.INotificationService _notificationService;

    public AppointmentService(ApplicationDbContext db, MediAlert.Services.Doctors.Interfaces.IZoomApiService zoomService, MediAlert.Services.Billing.Interfaces.IStripeBillingService billingService, MediAlert.Services.Notifications.INotificationService notificationService)
    {
        _db = db;
        _zoomService = zoomService;
        _billingService = billingService;
        _notificationService = notificationService;
    }

    private async Task NotifyCaregiversAsync(Guid patientId, string title, string message, string type, string? actionUrl, CancellationToken cancellationToken)
    {
        var caregiverUserIds = await _db.CaregiverPatientLinks
            .Include(l => l.Caregiver)
            .Where(l => l.PatientId == patientId && l.Status == LinkStatuses.Approved)
            .Select(l => l.Caregiver.UserId)
            .ToListAsync(cancellationToken);

        foreach (var caregiverUserId in caregiverUserIds)
        {
            if (!string.IsNullOrEmpty(caregiverUserId))
            {
                await _notificationService.SendNotificationAsync(
                    caregiverUserId,
                    title,
                    message,
                    type,
                    actionUrl,
                    cancellationToken);
            }
        }
    }

    public async Task<AppointmentServiceResult<AppointmentResponse>> BookAppointmentAsync(Guid patientId, CreateAppointmentRequest request, CancellationToken cancellationToken = default)
    {
        request.ScheduledDateTime = request.ScheduledDateTime.ToUniversalTime();

        var patient = await _db.Patients.Include(p => p.User).FirstOrDefaultAsync(p => p.PatientId == patientId || p.UserId == patientId.ToString(), cancellationToken);
        if (patient == null) return AppointmentServiceResult<AppointmentResponse>.Failure("PatientNotFound", "Patient profile not found.", StatusCodes.Status404NotFound);

        // Future date validation
        if (request.ScheduledDateTime <= DateTime.UtcNow)
        {
            return AppointmentServiceResult<AppointmentResponse>.Failure("InvalidDateTime", "Appointment time must be in the future.", StatusCodes.Status400BadRequest);
        }

        if (request.AppointmentType == "Video")
        {
            var hasPremium = await _billingService.HasPremiumAccessAsync(patientId, cancellationToken);
            if (!hasPremium)
            {
                return AppointmentServiceResult<AppointmentResponse>.Failure("PremiumRequired", "A premium subscription is required to book video consultations.", StatusCodes.Status403Forbidden);
            }
        }

        // Verify doctor status
        var doctor = await _db.Doctors.Include(d => d.User).FirstOrDefaultAsync(d => d.DoctorId == request.DoctorId, cancellationToken);
        if (doctor == null)
        {
            return AppointmentServiceResult<AppointmentResponse>.Failure("DoctorNotFound", "Doctor profile not found.", StatusCodes.Status404NotFound);
        }
        if (doctor.VerificationStatus != "Verified")
        {
            return AppointmentServiceResult<AppointmentResponse>.Failure("DoctorNotVerified", "This doctor is not verified and cannot accept appointments.", StatusCodes.Status400BadRequest);
        }

        // Slot conflict prevention
        var conflict = await _db.Consultations.AnyAsync(c => 
            c.DoctorId == doctor.DoctorId && 
            c.Status != "Cancelled" && 
            c.ScheduledDateTime == request.ScheduledDateTime, 
            cancellationToken);
        if (conflict)
        {
            return AppointmentServiceResult<AppointmentResponse>.Failure("SlotConflict", "This time slot is already booked.", StatusCodes.Status400BadRequest);
        }

        var appointment = new Consultation
        {
            ConsultationId = Guid.NewGuid(),
            PatientId = patient.PatientId,
            DoctorId = doctor.DoctorId,
            ScheduledDateTime = request.ScheduledDateTime,
            Type = request.AppointmentType,
            Status = AppointmentStatuses.Scheduled,
            CreatedAt = DateTime.UtcNow
        };

        if (request.AppointmentType.Contains("Video", StringComparison.OrdinalIgnoreCase))
        {
            var zoomResult = await _zoomService.CreateMeetingAsync($"Video Consultation with Dr. {doctor.UserId}", request.ScheduledDateTime, 30, cancellationToken);
            appointment.ZoomMeetingUrl = zoomResult.JoinUrl;
        }
        // Link patient and doctor if not already linked
        var linkExists = await _db.PatientDoctorLinks.AnyAsync(l => l.PatientId == patient.PatientId && l.DoctorId == doctor.DoctorId, cancellationToken);
        if (!linkExists)
        {
            await _db.PatientDoctorLinks.AddAsync(new PatientDoctorLink
            {
                PatientId = patient.PatientId,
                DoctorId = doctor.DoctorId,
                Status = LinkStatuses.Approved,
                ApprovedDate = DateTime.UtcNow
            }, cancellationToken);
        }

        await _db.Consultations.AddAsync(appointment, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        
        await _notificationService.SendNotificationAsync(
            doctor.UserId, 
            "New Consultation Booked", 
            $"A new {request.AppointmentType} consultation has been booked for {request.ScheduledDateTime.ToShortDateString()} at {request.ScheduledDateTime.ToShortTimeString()}.", 
            "Consultation", 
            "/doctor/dashboard", 
            cancellationToken);

        await _notificationService.SendNotificationAsync(
            patient.UserId, 
            "Consultation Confirmed", 
            $"Your {request.AppointmentType} consultation is confirmed for {request.ScheduledDateTime.ToShortDateString()} at {request.ScheduledDateTime.ToShortTimeString()}.", 
            "Consultation", 
            "/patient/dashboard", 
            cancellationToken);

        await NotifyCaregiversAsync(
            patient.PatientId,
            "Patient Consultation Booked",
            $"{patient.User?.FullName ?? "Your patient"} has booked a new {request.AppointmentType} consultation with Dr. {doctor.User?.FullName ?? "doctor"} for {request.ScheduledDateTime.ToShortDateString()} at {request.ScheduledDateTime.ToShortTimeString()}.",
            "Consultation",
            "/caregiver/dashboard",
            cancellationToken);

        return AppointmentServiceResult<AppointmentResponse>.Success(new AppointmentResponse { AppointmentId = appointment.ConsultationId, Status = appointment.Status });
    }


    public async Task<AppointmentServiceResult<AppointmentResponse>> RescheduleAppointmentAsync(Guid appointmentId, Guid patientId, DateTime newTime, CancellationToken cancellationToken = default)
    {
        newTime = newTime.Kind == DateTimeKind.Unspecified 
            ? DateTime.SpecifyKind(newTime, DateTimeKind.Utc) 
            : newTime.ToUniversalTime();

        var patient = await _db.Patients.Include(p => p.User).FirstOrDefaultAsync(p => p.PatientId == patientId || p.UserId == patientId.ToString(), cancellationToken);
        if (patient == null) return AppointmentServiceResult<AppointmentResponse>.Failure("PatientNotFound", "Patient profile not found.", StatusCodes.Status404NotFound);

        var appointment = await _db.Consultations.FirstOrDefaultAsync(c => c.ConsultationId == appointmentId && c.PatientId == patient.PatientId, cancellationToken);
        if (appointment == null) return AppointmentServiceResult<AppointmentResponse>.Failure(AppointmentErrorCodes.AppointmentNotFound, "Appointment not found.", StatusCodes.Status404NotFound);
        
        appointment.ScheduledDateTime = newTime;
        appointment.Status = AppointmentStatuses.Rescheduled;
        await _db.SaveChangesAsync(cancellationToken);
        
        var doctor = await _db.Doctors.Include(d => d.User).FirstOrDefaultAsync(d => d.DoctorId == appointment.DoctorId, cancellationToken);
        if (doctor != null)
        {
            await _notificationService.SendNotificationAsync(
                doctor.UserId, 
                "Consultation Rescheduled", 
                $"Your consultation has been rescheduled to {newTime.ToShortDateString()} at {newTime.ToShortTimeString()}.", 
                "Consultation", 
                "/doctor/dashboard", 
                cancellationToken);
        }

        await _notificationService.SendNotificationAsync(
            patient.UserId, 
            "Consultation Rescheduled", 
            $"Your consultation has been rescheduled to {newTime.ToShortDateString()} at {newTime.ToShortTimeString()}.", 
            "Consultation", 
            "/patient/dashboard", 
            cancellationToken);

        await NotifyCaregiversAsync(
            patient.PatientId,
            "Patient Consultation Rescheduled",
            $"Your patient {patient.User?.FullName ?? "patient"}'s consultation with Dr. {doctor?.User?.FullName ?? "doctor"} has been rescheduled to {newTime.ToShortDateString()} at {newTime.ToShortTimeString()}.",
            "Consultation",
            "/caregiver/dashboard",
            cancellationToken);

        return AppointmentServiceResult<AppointmentResponse>.Success(new AppointmentResponse { AppointmentId = appointment.ConsultationId, Status = appointment.Status });
    }

    public async Task<AppointmentServiceResult<AppointmentResponse>> CancelAppointmentAsync(Guid appointmentId, Guid patientId, CancellationToken cancellationToken = default)
    {
        var patient = await _db.Patients.Include(p => p.User).FirstOrDefaultAsync(p => p.PatientId == patientId || p.UserId == patientId.ToString(), cancellationToken);
        if (patient == null) return AppointmentServiceResult<AppointmentResponse>.Failure("PatientNotFound", "Patient profile not found.", StatusCodes.Status404NotFound);

        var appointment = await _db.Consultations.FirstOrDefaultAsync(c => c.ConsultationId == appointmentId && c.PatientId == patient.PatientId, cancellationToken);
        if (appointment == null) return AppointmentServiceResult<AppointmentResponse>.Failure(AppointmentErrorCodes.AppointmentNotFound, "Appointment not found.", StatusCodes.Status404NotFound);
        
        appointment.Status = AppointmentStatuses.Cancelled;
        await _db.SaveChangesAsync(cancellationToken);
        
        var doctor = await _db.Doctors.Include(d => d.User).FirstOrDefaultAsync(d => d.DoctorId == appointment.DoctorId, cancellationToken);
        if (doctor != null)
        {
            await _notificationService.SendNotificationAsync(
                doctor.UserId, 
                "Consultation Cancelled", 
                $"A consultation scheduled for {appointment.ScheduledDateTime.ToShortDateString()} at {appointment.ScheduledDateTime.ToShortTimeString()} has been cancelled.", 
                "Consultation", 
                "/doctor/dashboard", 
                cancellationToken);
        }

        await _notificationService.SendNotificationAsync(
            patient.UserId, 
            "Consultation Cancelled", 
            $"Your consultation scheduled for {appointment.ScheduledDateTime.ToShortDateString()} at {appointment.ScheduledDateTime.ToShortTimeString()} has been cancelled.", 
            "Consultation", 
            "/patient/dashboard", 
            cancellationToken);

        await NotifyCaregiversAsync(
            patient.PatientId,
            "Patient Consultation Cancelled",
            $"Your patient {patient.User?.FullName ?? "patient"}'s consultation with Dr. {doctor?.User?.FullName ?? "doctor"} scheduled for {appointment.ScheduledDateTime.ToShortDateString()} at {appointment.ScheduledDateTime.ToShortTimeString()} has been cancelled.",
            "Consultation",
            "/caregiver/dashboard",
            cancellationToken);

        return AppointmentServiceResult<AppointmentResponse>.Success(new AppointmentResponse { AppointmentId = appointment.ConsultationId, Status = appointment.Status });
    }

    public async Task<AppointmentServiceResult<bool>> GenerateRefillRemindersAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        var patient = await _db.Patients.FirstOrDefaultAsync(p => p.PatientId == patientId || p.UserId == patientId.ToString(), cancellationToken);
        if (patient == null) return AppointmentServiceResult<bool>.Failure("PatientNotFound", "Patient profile not found.", StatusCodes.Status404NotFound);

        // Simple logic: Scan active medications. If no refill reminder exists, create one.
        var activeMeds = await _db.Medications
            .Where(m => m.PatientId == patient.PatientId && m.IsActive)
            .ToListAsync(cancellationToken);

        var existingReminders = await _db.RefillReminders
            .Where(r => r.PatientId == patient.PatientId && r.Status == "Pending")
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
                    PatientId = patient.PatientId,
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
        var patient = await _db.Patients.FirstOrDefaultAsync(p => p.PatientId == patientId || p.UserId == patientId.ToString(), cancellationToken);
        if (patient == null) return AppointmentServiceResult<bool>.Failure("PatientNotFound", "Patient profile not found.", StatusCodes.Status404NotFound);

        var reminder = await _db.RefillReminders.FirstOrDefaultAsync(r => r.ReminderId == reminderId && r.PatientId == patient.PatientId, cancellationToken);
        if (reminder == null) return AppointmentServiceResult<bool>.Failure("RefillError", "Reminder not found.", StatusCodes.Status404NotFound);

        reminder.Status = "Acknowledged";
        await _db.SaveChangesAsync(cancellationToken);
        return AppointmentServiceResult<bool>.Success(true);
    }

    public async Task<AppointmentServiceResult<List<RefillReminderDto>>> GetPatientRefillsAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        var patient = await _db.Patients.FirstOrDefaultAsync(p => p.PatientId == patientId || p.UserId == patientId.ToString(), cancellationToken);
        if (patient == null) return AppointmentServiceResult<List<RefillReminderDto>>.Success(new());

        var refills = await _db.RefillReminders
            .Include(r => r.Medication)
            .Where(r => r.PatientId == patient.PatientId)
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
        var consultation = await _db.Consultations
            .Include(c => c.Patient)
                .ThenInclude(p => p.User)
            .Include(c => c.Doctor)
                .ThenInclude(d => d.User)
            .FirstOrDefaultAsync(c => c.ConsultationId == appointmentId, cancellationToken);
        if (consultation == null) return AppointmentServiceResult<bool>.Failure("NotFound", "Consultation not found.", StatusCodes.Status404NotFound);

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

        if (consultation.Patient != null && !string.IsNullOrEmpty(consultation.Patient.UserId))
        {
            await _notificationService.SendNotificationAsync(
                consultation.Patient.UserId,
                "Consultation Notes Added",
                "Your doctor has added notes/reports to your recent consultation.",
                "Consultation",
                "/patient/dashboard",
                cancellationToken);

            await NotifyCaregiversAsync(
                consultation.Patient.PatientId,
                "Patient Consultation Notes Added",
                $"Dr. {consultation.Doctor?.User?.FullName ?? "doctor"} has added notes to {consultation.Patient.User?.FullName ?? "your patient"}'s recent consultation.",
                "Consultation",
                "/caregiver/dashboard",
                cancellationToken);
        }

        return AppointmentServiceResult<bool>.Success(true);
    }

    public async Task<AppointmentServiceResult<List<AppointmentSummaryResponse>>> GetPatientAppointmentsAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        var patient = await _db.Patients.FirstOrDefaultAsync(p => p.PatientId == patientId || p.UserId == patientId.ToString(), cancellationToken);
        if (patient == null) return AppointmentServiceResult<List<AppointmentSummaryResponse>>.Success(new());

        var consultations = await _db.Consultations
            .AsNoTracking()
            .Where(c => c.PatientId == patient.PatientId)
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
