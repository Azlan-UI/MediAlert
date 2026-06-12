using MediAlert.DTOs.Doctors;

namespace MediAlert.Services.Doctors.Interfaces;

public interface IDoctorService
{
    Task<DoctorServiceResult<bool>> AddAvailabilityAsync(Guid doctorId, CreateDoctorAvailabilityRequest request, CancellationToken cancellationToken = default);
    Task<DoctorServiceResult<List<DoctorAvailabilityResponse>>> GetAvailabilitiesAsync(Guid doctorId, CancellationToken cancellationToken = default);
    Task<DoctorServiceResult<bool>> RemoveAvailabilityAsync(Guid doctorId, Guid availabilityId, CancellationToken cancellationToken = default);
    Task<DoctorServiceResult<ConsultationResponse>> BookConsultationAsync(Guid patientId, Guid doctorId, DateTime scheduledTime, string type, CancellationToken cancellationToken = default);
    Task<DoctorServiceResult<List<DoctorResponse>>> SearchDoctorsAsync(string? specialization, CancellationToken cancellationToken = default);
    Task<DoctorServiceResult<DoctorResponse>> GetDoctorProfileAsync(Guid doctorId, CancellationToken cancellationToken = default);
    Task<DoctorServiceResult<DoctorDashboardResponse>> GetDashboardDataAsync(string userId, CancellationToken cancellationToken = default);

    Task<DoctorServiceResult<ConsultationDetailsResponse>> GetConsultationDetailsAsync(Guid consultationId, string userId, CancellationToken cancellationToken = default);
    Task<DoctorServiceResult<List<DateTime>>> GetBookedSlotsAsync(Guid doctorId, DateTime date, CancellationToken cancellationToken = default);
}
