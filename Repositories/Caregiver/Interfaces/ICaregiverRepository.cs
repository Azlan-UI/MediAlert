using MediAlert.Models;

namespace MediAlert.Repositories.Caregiver.Interfaces;

public interface ICaregiverRepository
{
    Task<Models.Caregiver?> GetCaregiverAsync(Guid caregiverId, CancellationToken cancellationToken = default);
    Task<List<CaregiverPatientLink>> GetPendingLinkRequestsAsync(Guid caregiverId, CancellationToken cancellationToken = default);
    Task<List<CaregiverPatientLink>> GetApprovedPatientsAsync(Guid caregiverId, CancellationToken cancellationToken = default);
    Task<CaregiverPatientLink?> GetLinkAsync(Guid linkId, CancellationToken cancellationToken = default);
    Task AddLinkRequestAsync(CaregiverPatientLink linkRequest, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
