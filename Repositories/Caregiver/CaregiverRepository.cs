using MediAlert.Data;
using MediAlert.Models;
using MediAlert.Repositories.Caregiver.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MediAlert.Repositories.Caregiver;

public sealed class CaregiverRepository : ICaregiverRepository
{
    private readonly ApplicationDbContext _dbContext;

    public CaregiverRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Models.Caregiver?> GetCaregiverAsync(Guid caregiverId, CancellationToken cancellationToken = default) =>
        _dbContext.Caregivers
            .AsTracking()
            .FirstOrDefaultAsync(c => c.CaregiverId == caregiverId, cancellationToken);

    public Task<List<CaregiverPatientLink>> GetPendingLinkRequestsAsync(Guid caregiverId, CancellationToken cancellationToken = default) =>
        _dbContext.CaregiverPatientLinks
            .Include(l => l.Patient)
            .AsNoTracking()
            .Where(l => l.CaregiverId == caregiverId && l.Status == "Pending")
            .ToListAsync(cancellationToken);

    public Task<List<CaregiverPatientLink>> GetApprovedPatientsAsync(Guid caregiverId, CancellationToken cancellationToken = default) =>
        _dbContext.CaregiverPatientLinks
            .Include(l => l.Patient)
            .AsNoTracking()
            .Where(l => l.CaregiverId == caregiverId && l.Status == "Approved")
            .ToListAsync(cancellationToken);

    public Task<CaregiverPatientLink?> GetLinkAsync(Guid linkId, CancellationToken cancellationToken = default) =>
        _dbContext.CaregiverPatientLinks
            .AsTracking()
            .FirstOrDefaultAsync(l => l.CaregiverPatientLinkId == linkId, cancellationToken);

    public async Task AddLinkRequestAsync(CaregiverPatientLink linkRequest, CancellationToken cancellationToken = default)
    {
        await _dbContext.CaregiverPatientLinks.AddAsync(linkRequest, cancellationToken);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
