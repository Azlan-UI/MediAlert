using MediAlert.DTOs.Admin;

namespace MediAlert.Services.Admin.Interfaces;

public interface IAdminService
{
    Task<List<AdminUserOverviewDto>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task<bool> ToggleUserSuspensionAsync(string userId, CancellationToken cancellationToken = default);
    Task<string?> DeleteUserAsync(string userId, CancellationToken cancellationToken = default);

    Task<List<AdminDoctorOverviewDto>> GetDoctorsAsync(CancellationToken cancellationToken = default);
    Task<bool> VerifyDoctorAsync(Guid doctorId, string status, string? rejectionReason = null, Guid? adminUserId = null, CancellationToken cancellationToken = default);

    Task<List<AdminConsultationOverviewDto>> GetConsultationsAsync(CancellationToken cancellationToken = default);
    Task<bool> CancelConsultationAsync(Guid consultationId, CancellationToken cancellationToken = default);

    Task<List<AdminSubscriptionOverviewDto>> GetSubscriptionsAsync(CancellationToken cancellationToken = default);
}
