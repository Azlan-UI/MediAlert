
using MediAlert.Constants;
using MediAlert.Data;
using MediAlert.DTOs.Doctors;
using MediAlert.Models;
using MediAlert.Services.Doctors.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace MediAlert.Services.Doctors;

public sealed class DoctorService : IDoctorService
{
    private readonly ApplicationDbContext _db;
    private readonly IZoomApiService _zoomApiService;

    public DoctorService(ApplicationDbContext db, IZoomApiService zoomApiService)
    {
        _db = db;
        _zoomApiService = zoomApiService;
    }

    public async Task<DoctorServiceResult<bool>> AddAvailabilityAsync(Guid doctorId, CreateDoctorAvailabilityRequest request, CancellationToken cancellationToken = default)
    {
        var startTime = request.StartTime.ToTimeSpan();
        var endTime = request.EndTime.ToTimeSpan();
        if (endTime <= startTime) return DoctorServiceResult<bool>.Failure(DoctorErrorCodes.InvalidTimeRange, "End time must be after start time.");
        
        var doctor = await _db.Doctors.FirstOrDefaultAsync(d => d.DoctorId == doctorId || d.UserId == doctorId.ToString(), cancellationToken);
        if (doctor == null) return DoctorServiceResult<bool>.Failure("DoctorNotFound", "Doctor profile not found.", StatusCodes.Status404NotFound);

        var actualDoctorId = doctor.DoctorId;

        var availabilities = await _db.DoctorAvailabilities.Where(a => a.DoctorId == actualDoctorId).ToListAsync(cancellationToken);
        
        var overlap = availabilities.Any(a => 
            (
                (request.IsRecurring && a.DayOfWeek == (DayOfWeek?)request.DayOfWeek) ||
                (!request.IsRecurring && a.SpecificDate?.Date == request.SpecificDate?.Date)
            ) && 
            startTime < a.EndTime && endTime > a.StartTime);

             
        if (overlap) return DoctorServiceResult<bool>.Failure(DoctorErrorCodes.OverlappingSlot, "Overlapping availability slot.");

        var availability = new DoctorAvailability
        {
            DoctorId = actualDoctorId,
            DayOfWeek = request.IsRecurring ? (DayOfWeek?)request.DayOfWeek : null,
            SpecificDate = request.IsRecurring ? null : (request.SpecificDate.HasValue ? DateTime.SpecifyKind(request.SpecificDate.Value.Date, DateTimeKind.Utc) : null),
            StartTime = startTime,
            EndTime = endTime
        };
        
        await _db.DoctorAvailabilities.AddAsync(availability, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return DoctorServiceResult<bool>.Success(true);
    }

    public async Task<DoctorServiceResult<List<DoctorAvailabilityResponse>>> GetAvailabilitiesAsync(Guid doctorId, CancellationToken cancellationToken = default)
    {
        var doctor = await _db.Doctors.FirstOrDefaultAsync(d => d.DoctorId == doctorId || d.UserId == doctorId.ToString(), cancellationToken);
        if (doctor == null)
            return DoctorServiceResult<List<DoctorAvailabilityResponse>>.Success(new());

        var dbAvailabilities = await _db.DoctorAvailabilities
            .Where(a => a.DoctorId == doctor.DoctorId)
            .OrderBy(a => a.SpecificDate).ThenBy(a => a.DayOfWeek).ThenBy(a => a.StartTime)
            .ToListAsync(cancellationToken);

        var responses = dbAvailabilities.Select(a => new DoctorAvailabilityResponse
        {
            AvailabilityId = a.DoctorAvailabilityId,
            DoctorId = a.DoctorId,
            DayOfWeek = a.DayOfWeek.HasValue ? (int)a.DayOfWeek.Value : null,
            SpecificDate = a.SpecificDate,
            StartTime = TimeOnly.FromTimeSpan(a.StartTime),
            EndTime = TimeOnly.FromTimeSpan(a.EndTime),
            IsRecurring = !a.SpecificDate.HasValue
        }).ToList();
            
        return DoctorServiceResult<List<DoctorAvailabilityResponse>>.Success(responses);
    }

    public async Task<DoctorServiceResult<bool>> RemoveAvailabilityAsync(Guid doctorId, Guid availabilityId, CancellationToken cancellationToken = default)
    {
        var doctor = await _db.Doctors.FirstOrDefaultAsync(d => d.DoctorId == doctorId || d.UserId == doctorId.ToString(), cancellationToken);
        if (doctor == null) return DoctorServiceResult<bool>.Failure("DoctorNotFound", "Doctor profile not found.", StatusCodes.Status404NotFound);

        var availability = await _db.DoctorAvailabilities.FirstOrDefaultAsync(a => a.DoctorAvailabilityId == availabilityId && a.DoctorId == doctor.DoctorId, cancellationToken);
        if (availability == null)
            return DoctorServiceResult<bool>.Failure("AvailabilityNotFound", "Availability slot not found or access denied.", StatusCodes.Status404NotFound);
            
        _db.DoctorAvailabilities.Remove(availability);
        await _db.SaveChangesAsync(cancellationToken);
        return DoctorServiceResult<bool>.Success(true);
    }

    public async Task<DoctorServiceResult<ConsultationResponse>> BookConsultationAsync(Guid patientId, Guid doctorId, DateTime scheduledTime, string type, CancellationToken cancellationToken = default)
    {
        scheduledTime = scheduledTime.Kind == DateTimeKind.Unspecified 
            ? DateTime.SpecifyKind(scheduledTime, DateTimeKind.Utc) 
            : scheduledTime.ToUniversalTime();

        var patient = await _db.Patients.FirstOrDefaultAsync(p => p.PatientId == patientId || p.UserId == patientId.ToString(), cancellationToken);
        if (patient == null) return DoctorServiceResult<ConsultationResponse>.Failure("PatientNotFound", "Patient profile not found.", StatusCodes.Status404NotFound);
        var actualPatientId = patient.PatientId;

        var doctor = await _db.Doctors.FirstOrDefaultAsync(d => d.DoctorId == doctorId || d.UserId == doctorId.ToString(), cancellationToken);
        if (doctor == null) return DoctorServiceResult<ConsultationResponse>.Failure("DoctorNotFound", "Doctor profile not found.", StatusCodes.Status404NotFound);
        if (doctor.VerificationStatus != "Verified") return DoctorServiceResult<ConsultationResponse>.Failure("DoctorNotVerified", "This doctor is not verified and cannot accept appointments.", StatusCodes.Status400BadRequest);

        var actualDoctorId = doctor.DoctorId;

        // Slot conflict prevention
        var conflict = await _db.Consultations.AnyAsync(c => 
            c.DoctorId == actualDoctorId && 
            c.Status != "Cancelled" && 
            c.ScheduledDateTime == scheduledTime, 
            cancellationToken);
        if (conflict)
        {
            return DoctorServiceResult<ConsultationResponse>.Failure("SlotConflict", "This time slot is already booked.", StatusCodes.Status400BadRequest);
        }

        var consultation = new Consultation
        {
            ConsultationId = Guid.NewGuid(),
            PatientId = actualPatientId,
            DoctorId = actualDoctorId,
            ScheduledDateTime = scheduledTime,
            Type = type,
            Status = ConsultationStatuses.Scheduled,
            CreatedAt = DateTime.UtcNow
        };

        if (type == "Video")
        {
            var zoom = await _zoomApiService.CreateMeetingAsync($"Consultation with Patient {actualPatientId}", scheduledTime, 30, cancellationToken);
            consultation.ZoomMeetingUrl = zoom.JoinUrl;
        }
        
        // Link patient and doctor if not already linked
        var linkExists = await _db.PatientDoctorLinks.AnyAsync(l => l.PatientId == actualPatientId && l.DoctorId == actualDoctorId, cancellationToken);
        if (!linkExists)
        {
            await _db.PatientDoctorLinks.AddAsync(new PatientDoctorLink
            {
                PatientId = actualPatientId,
                DoctorId = actualDoctorId,
                Status = LinkStatuses.Approved,
                ApprovedDate = DateTime.UtcNow
            }, cancellationToken);
        }
        await _db.Consultations.AddAsync(consultation, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return DoctorServiceResult<ConsultationResponse>.Success(new ConsultationResponse { ConsultationId = consultation.ConsultationId, Status = consultation.Status });
    }


    public async Task<DoctorServiceResult<List<DoctorResponse>>> SearchDoctorsAsync(string? specialization, CancellationToken cancellationToken = default)
    {
        var query = _db.Doctors.AsNoTracking().Where(d => d.VerificationStatus == "Verified");
        if (!string.IsNullOrEmpty(specialization))
        {
            var searchStr = specialization.Trim().ToLower();
            if (searchStr.StartsWith("dr. ")) searchStr = searchStr.Substring(4).Trim();
            else if (searchStr.StartsWith("dr ")) searchStr = searchStr.Substring(3).Trim();
            else if (searchStr.StartsWith("doctor ")) searchStr = searchStr.Substring(7).Trim();

            query = query.Where(d => 
                (d.Specialization != null && d.Specialization.ToLower().Contains(searchStr)) || 
                (d.User != null && d.User.FullName != null && d.User.FullName.ToLower().Contains(searchStr)));
        }
        
        var doctors = await query.Include(d => d.User).ToListAsync(cancellationToken);
        var responses = doctors.Select(d => new DoctorResponse 
        { 
            DoctorId = d.DoctorId,
            UserId = d.UserId,
            FullName = d.User?.FullName ?? "Unknown",
            Email = d.User?.Email ?? string.Empty,
            LicenseNumber = d.LicenseNumber,
            Specialization = d.Specialization,
            Qualifications = d.Qualifications,
            ExperienceYears = d.YearsOfExperience,
            IsVerified = d.VerificationStatus == "Verified",
            VerificationStatus = d.VerificationStatus,
            ClinicName = d.ClinicName,
            ContactInfo = d.ContactInfo,
            Biography = d.Biography,
            ProfilePhotoUrl = d.ProfilePhotoUrl,
            CreatedAt = d.CreatedAt
        }).ToList();
        return DoctorServiceResult<List<DoctorResponse>>.Success(responses);
    }

    public async Task<DoctorServiceResult<DoctorResponse>> GetDoctorProfileAsync(Guid doctorId, CancellationToken cancellationToken = default)
    {
        var doctor = await _db.Doctors
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.DoctorId == doctorId || d.UserId == doctorId.ToString(), cancellationToken);

        if (doctor == null)
            return DoctorServiceResult<DoctorResponse>.Failure("DoctorNotFound", "Doctor profile not found.", StatusCodes.Status404NotFound);

        var response = new DoctorResponse
        {
            DoctorId = doctor.DoctorId,
            UserId = doctor.UserId,
            FullName = doctor.User?.FullName ?? "Unknown",
            Email = doctor.User?.Email ?? string.Empty,
            LicenseNumber = doctor.LicenseNumber,
            Specialization = doctor.Specialization,
            Qualifications = doctor.Qualifications,
            ExperienceYears = doctor.YearsOfExperience,
            IsVerified = doctor.VerificationStatus == "Verified",
            VerificationStatus = doctor.VerificationStatus,
            ClinicName = doctor.ClinicName,
            ContactInfo = doctor.ContactInfo,
            Biography = doctor.Biography,
            ProfilePhotoUrl = doctor.ProfilePhotoUrl,
            CreatedAt = doctor.CreatedAt
        };

        return DoctorServiceResult<DoctorResponse>.Success(response);
    }


    public async Task<DoctorServiceResult<DoctorDashboardResponse>> GetDashboardDataAsync(string userId, CancellationToken cancellationToken = default)
    {
        var doctor = await _db.Doctors
            .Include(d => d.User)
            .Include(d => d.LinkedPatients)
            .Include(d => d.Consultations)
                .ThenInclude(c => c.Patient)
                    .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(d => d.UserId == userId, cancellationToken);

        if (doctor == null)
            return DoctorServiceResult<DoctorDashboardResponse>.Failure("DoctorProfileNotFound", "Doctor profile not found.", StatusCodes.Status404NotFound);

        var today = DateTime.UtcNow.Date;
        
        var todaysAppointments = doctor.Consultations
            .Count(c => c.ScheduledDateTime.Date == today && c.Status != "Cancelled");

        var pendingConsultations = doctor.Consultations
            .Count(c => c.Status == "Scheduled");

        var totalPatients = doctor.LinkedPatients
            .Select(l => l.PatientId)
            .Distinct()
            .Count();

        var upcoming = doctor.Consultations
            .Where(c => c.ScheduledDateTime >= DateTime.UtcNow && c.Status == "Scheduled")
            .OrderBy(c => c.ScheduledDateTime)
            .Take(10)
            .Select(c => new ConsultationSummaryResponse
            {
                ConsultationId = c.ConsultationId,
                PatientId = c.PatientId,
                DoctorId = c.DoctorId,
                ScheduledTime = c.ScheduledDateTime,
                Status = c.Status,
                Type = c.Type,
                ZoomMeetingUrl = c.ZoomMeetingUrl,
                PatientFullName = c.Patient.User.FullName,
                DoctorFullName = doctor.User?.FullName ?? "Unknown"
            })
            .ToList();

        var response = new DoctorDashboardResponse
        {
            TodaysAppointmentsCount = todaysAppointments,
            PendingConsultationsCount = pendingConsultations,
            TotalPatientsCount = totalPatients,
            VerificationStatus = doctor.VerificationStatus,
            RejectionReason = doctor.RejectionReason,
            UpcomingAppointments = upcoming
        };

        return DoctorServiceResult<DoctorDashboardResponse>.Success(response);
    }

    public async Task<DoctorServiceResult<ConsultationDetailsResponse>> GetConsultationDetailsAsync(Guid consultationId, string userId, CancellationToken cancellationToken = default)
    {
        var consultation = await _db.Consultations
            .Include(c => c.Patient)
                .ThenInclude(p => p.User)
            .Include(c => c.Doctor)
                .ThenInclude(d => d.User)
            .Include(c => c.ConsultationNote)
            .FirstOrDefaultAsync(c => c.ConsultationId == consultationId, cancellationToken);

        if (consultation == null)
            return DoctorServiceResult<ConsultationDetailsResponse>.Failure("ConsultationNotFound", "Consultation not found.", StatusCodes.Status404NotFound);

        if (consultation.Doctor.UserId != userId && consultation.Patient.UserId != userId)
            return DoctorServiceResult<ConsultationDetailsResponse>.Failure("UnauthorizedAccess", "Unauthorized access.", StatusCodes.Status403Forbidden);

        var response = new ConsultationDetailsResponse
        {
            ConsultationId = consultation.ConsultationId,
            PatientId = consultation.PatientId,
            PatientFullName = consultation.Patient.User.FullName,
            DoctorId = consultation.DoctorId,
            DoctorFullName = consultation.Doctor.User.FullName,
            DoctorSpecialization = consultation.Doctor.Specialization,
            ScheduledTime = consultation.ScheduledDateTime,
            Status = consultation.Status,
            Type = consultation.Type,
            ZoomMeetingUrl = consultation.ZoomMeetingUrl,
            Note = consultation.ConsultationNote != null ? new ConsultationNoteResponse
            {
                NoteId = consultation.ConsultationNote.ConsultationNoteId,
                ConsultationId = consultation.ConsultationNote.ConsultationId,
                DoctorObservations = consultation.ConsultationNote.ClinicalNotes,
                Prescription = consultation.ConsultationNote.Prescriptions,
                CreatedAt = consultation.ConsultationNote.CreatedAt
            } : null
        };

        return DoctorServiceResult<ConsultationDetailsResponse>.Success(response);
    }

    public async Task<DoctorServiceResult<List<DateTime>>> GetBookedSlotsAsync(Guid doctorId, DateTime date, CancellationToken cancellationToken = default)
    {
        var doctor = await _db.Doctors.FirstOrDefaultAsync(d => d.DoctorId == doctorId || d.UserId == doctorId.ToString(), cancellationToken);
        if (doctor == null) return DoctorServiceResult<List<DateTime>>.Failure("DoctorNotFound", "Doctor profile not found.", StatusCodes.Status404NotFound);

        var startOfDay = date.Kind == DateTimeKind.Unspecified 
            ? DateTime.SpecifyKind(date.Date, DateTimeKind.Utc) 
            : date.ToUniversalTime().Date;
        var endOfDay = startOfDay.AddDays(1);

        var booked = await _db.Consultations
            .Where(c => c.DoctorId == doctor.DoctorId && 
                        c.Status != "Cancelled" && 
                        c.ScheduledDateTime >= startOfDay && 
                        c.ScheduledDateTime < endOfDay)
            .Select(c => c.ScheduledDateTime)
            .ToListAsync(cancellationToken);

        return DoctorServiceResult<List<DateTime>>.Success(booked);
    }
}
