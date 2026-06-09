using MediAlert.DTOs.HealthProfile;

namespace MediAlert.Services.HealthProfile.Interfaces;

public interface IHealthProfileService
{
    Task<List<HealthConditionDto>> GetConditionsAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<HealthConditionDto?> GetConditionByIdAsync(Guid patientId, Guid conditionId, CancellationToken cancellationToken = default);
    Task<HealthConditionDto> AddConditionAsync(Guid patientId, CreateHealthConditionRequest request, CancellationToken cancellationToken = default);
    Task<HealthConditionDto?> UpdateConditionAsync(Guid patientId, Guid conditionId, UpdateHealthConditionRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteConditionAsync(Guid patientId, Guid conditionId, CancellationToken cancellationToken = default);
}
