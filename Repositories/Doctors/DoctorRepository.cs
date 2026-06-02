using MediAlert.Data;
using MediAlert.Models;
using MediAlert.Repositories.Doctors.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MediAlert.Repositories.Doctors;

public sealed class DoctorRepository : IDoctorRepository
{
    private readonly ApplicationDbContext _dbContext;

    public DoctorRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Doctor?> GetDoctorAsync(Guid doctorId, CancellationToken cancellationToken = default) =>
        _dbContext.Doctors
            .AsTracking()
            .FirstOrDefaultAsync(d => d.DoctorId == doctorId, cancellationToken);

    public Task<List<Doctor>> GetVerifiedDoctorsAsync(CancellationToken cancellationToken = default) =>
        _dbContext.Doctors
            .AsNoTracking()
            .Where(d => d.VerificationStatus == "Verified")
            .ToListAsync(cancellationToken);

    public Task<List<DoctorAvailability>> GetAvailabilityAsync(Guid doctorId, CancellationToken cancellationToken = default) =>
        _dbContext.DoctorAvailabilities
            .AsNoTracking()
            .Where(a => a.DoctorId == doctorId)
            .ToListAsync(cancellationToken);

    public async Task AddAvailabilityAsync(DoctorAvailability availability, CancellationToken cancellationToken = default)
    {
        await _dbContext.DoctorAvailabilities.AddAsync(availability, cancellationToken);
    }

    public Task<Consultation?> GetConsultationAsync(Guid consultationId, CancellationToken cancellationToken = default) =>
        _dbContext.Consultations
            .Include(c => c.ConsultationNote)
            .AsTracking()
            .FirstOrDefaultAsync(c => c.ConsultationId == consultationId, cancellationToken);

    public Task<List<Consultation>> GetConsultationsForPatientAsync(Guid patientId, CancellationToken cancellationToken = default) =>
        _dbContext.Consultations
            .Include(c => c.Doctor)
            .AsNoTracking()
            .Where(c => c.PatientId == patientId)
            .OrderByDescending(c => c.ScheduledDateTime)
            .ToListAsync(cancellationToken);

    public Task<List<Consultation>> GetConsultationsForDoctorAsync(Guid doctorId, CancellationToken cancellationToken = default) =>
        _dbContext.Consultations
            .Include(c => c.Patient)
            .AsNoTracking()
            .Where(c => c.DoctorId == doctorId)
            .OrderByDescending(c => c.ScheduledDateTime)
            .ToListAsync(cancellationToken);

    public async Task AddConsultationAsync(Consultation consultation, CancellationToken cancellationToken = default)
    {
        await _dbContext.Consultations.AddAsync(consultation, cancellationToken);
    }

    public async Task AddConsultationNoteAsync(ConsultationNote note, CancellationToken cancellationToken = default)
    {
        await _dbContext.ConsultationNotes.AddAsync(note, cancellationToken);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
