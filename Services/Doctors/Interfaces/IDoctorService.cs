using MediAlert.DTOs.Doctors;

namespace MediAlert.Services.Doctors.Interfaces;

public interface IDoctorService
{
    Task<DoctorServiceResult<DoctorResponse>> VerifyDoctorAsync(Guid doctorId, CancellationToken cancellationToken = default);
    Task<DoctorServiceResult<bool>> AddAvailabilityAsync(Guid doctorId, DayOfWeek dayOfWeek, TimeSpan startTime, TimeSpan endTime, CancellationToken cancellationToken = default);
    Task<DoctorServiceResult<ConsultationResponse>> BookConsultationAsync(Guid patientId, Guid doctorId, DateTime scheduledTime, string type, CancellationToken cancellationToken = default);
    Task<DoctorServiceResult<List<DoctorResponse>>> SearchDoctorsAsync(string? specialization, CancellationToken cancellationToken = default);
}
