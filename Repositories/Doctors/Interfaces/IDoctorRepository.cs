using MediAlert.Models;

namespace MediAlert.Repositories.Doctors.Interfaces;

public interface IDoctorRepository
{
    Task<Doctor?> GetDoctorAsync(Guid doctorId, CancellationToken cancellationToken = default);
    Task<List<Doctor>> GetVerifiedDoctorsAsync(CancellationToken cancellationToken = default);
    Task<List<DoctorAvailability>> GetAvailabilityAsync(Guid doctorId, CancellationToken cancellationToken = default);
    Task AddAvailabilityAsync(DoctorAvailability availability, CancellationToken cancellationToken = default);
    
    Task<Consultation?> GetConsultationAsync(Guid consultationId, CancellationToken cancellationToken = default);
    Task<List<Consultation>> GetConsultationsForPatientAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<List<Consultation>> GetConsultationsForDoctorAsync(Guid doctorId, CancellationToken cancellationToken = default);
    Task AddConsultationAsync(Consultation consultation, CancellationToken cancellationToken = default);
    
    Task AddConsultationNoteAsync(ConsultationNote note, CancellationToken cancellationToken = default);
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
