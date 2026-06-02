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

    public DoctorService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<DoctorServiceResult<DoctorResponse>> VerifyDoctorAsync(Guid doctorId, CancellationToken cancellationToken = default)
    {
        var doctor = await _db.Doctors.FirstOrDefaultAsync(d => d.DoctorId == doctorId, cancellationToken);
        if (doctor == null) return DoctorServiceResult<DoctorResponse>.Failure(DoctorErrorCodes.DoctorNotFound, "Doctor not found.", StatusCodes.Status404NotFound);
        
        doctor.VerificationStatus = VerificationStatuses.Verified;
        await _db.SaveChangesAsync(cancellationToken);
        
        return DoctorServiceResult<DoctorResponse>.Success(new DoctorResponse { DoctorId = doctor.DoctorId, VerificationStatus = doctor.VerificationStatus });
    }

    public async Task<DoctorServiceResult<bool>> AddAvailabilityAsync(Guid doctorId, DayOfWeek dayOfWeek, TimeSpan startTime, TimeSpan endTime, CancellationToken cancellationToken = default)
    {
        if (endTime <= startTime) return DoctorServiceResult<bool>.Failure(DoctorErrorCodes.InvalidTimeRange, "End time must be after start time.");
        
        var availabilities = await _db.DoctorAvailabilities.Where(a => a.DoctorId == doctorId).ToListAsync(cancellationToken);
        var overlap = availabilities.Any(a => a.DayOfWeek == dayOfWeek && 
            ((startTime >= a.StartTime && startTime < a.EndTime) || 
             (endTime > a.StartTime && endTime <= a.EndTime)));
             
        if (overlap) return DoctorServiceResult<bool>.Failure(DoctorErrorCodes.OverlappingSlot, "Overlapping availability slot.");

        var availability = new DoctorAvailability
        {
            DoctorId = doctorId,
            DayOfWeek = dayOfWeek,
            StartTime = startTime,
            EndTime = endTime
        };
        
        await _db.DoctorAvailabilities.AddAsync(availability, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return DoctorServiceResult<bool>.Success(true);
    }

    public async Task<DoctorServiceResult<ConsultationResponse>> BookConsultationAsync(Guid patientId, Guid doctorId, DateTime scheduledTime, string type, CancellationToken cancellationToken = default)
    {
        var consultation = new Consultation
        {
            ConsultationId = Guid.NewGuid(),
            PatientId = patientId,
            DoctorId = doctorId,
            ScheduledDateTime = scheduledTime,
            Type = type,
            Status = ConsultationStatuses.Scheduled,
            CreatedAt = DateTime.UtcNow
        };
        
        await _db.Consultations.AddAsync(consultation, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return DoctorServiceResult<ConsultationResponse>.Success(new ConsultationResponse { ConsultationId = consultation.ConsultationId, Status = consultation.Status });
    }

    public async Task<DoctorServiceResult<List<DoctorResponse>>> SearchDoctorsAsync(string? specialization, CancellationToken cancellationToken = default)
    {
        var query = _db.Doctors.AsNoTracking().Where(d => d.VerificationStatus == VerificationStatuses.Verified);
        if (!string.IsNullOrEmpty(specialization))
        {
            query = query.Where(d => d.Specialization.Contains(specialization));
        }
        
        var doctors = await query.ToListAsync(cancellationToken);
        var responses = doctors.Select(d => new DoctorResponse { DoctorId = d.DoctorId, Specialization = d.Specialization }).ToList();
        return DoctorServiceResult<List<DoctorResponse>>.Success(responses);
    }
}
