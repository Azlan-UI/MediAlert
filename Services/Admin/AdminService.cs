using MediAlert.Data;
using MediAlert.DTOs.Admin;
using MediAlert.Services.Admin.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MediAlert.Services.Admin;

public class AdminService : IAdminService
{
    private readonly ApplicationDbContext _db;

    public AdminService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<AdminUserOverviewDto>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Users
            .AsNoTracking()
            .Select(u => new AdminUserOverviewDto
            {
                UserId = u.Id,
                FullName = u.FullName,
                Email = u.Email ?? "",
                Role = u.Role,
                IsEmailVerified = u.IsEmailVerified,
                IsSuspended = u.IsSuspended,
                CreatedDate = u.CreatedDate
            })
            .OrderByDescending(u => u.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ToggleUserSuspensionAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null || user.Role == "Admin") // Prevent suspending admins
            return false;

        user.IsSuspended = !user.IsSuspended;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<string?> DeleteUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null) return "User not found.";
        if (user.Role == "Admin") return "Cannot delete admin accounts.";

        // Check active subscriptions
        var hasActiveSub = await _db.Subscriptions
            .Include(s => s.Patient)
            .AnyAsync(s => s.Patient.UserId == userId && (s.Status == "active" || s.Status == "trialing"), cancellationToken);

        if (hasActiveSub)
            return "Cannot delete user with an active subscription.";

        _db.Users.Remove(user);
        await _db.SaveChangesAsync(cancellationToken);
        return null; // success
    }

    public async Task<List<AdminDoctorOverviewDto>> GetDoctorsAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Doctors
            .Include(d => d.User)
            .AsNoTracking()
            .Select(d => new AdminDoctorOverviewDto
            {
                DoctorId = d.DoctorId,
                UserId = d.UserId,
                FullName = d.User.FullName,
                Email = d.User.Email ?? "",
                LicenseNumber = d.LicenseNumber,
                Specialization = d.Specialization,
                Qualifications = d.Qualifications,
                YearsOfExperience = d.YearsOfExperience,
                VerificationStatus = d.VerificationStatus,
                CreatedAt = d.CreatedAt
            })
            .OrderBy(d => d.VerificationStatus == "Pending" ? 0 : 1) // Pending first
            .ThenByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> VerifyDoctorAsync(Guid doctorId, string status, CancellationToken cancellationToken = default)
    {
        if (status != "Verified" && status != "Rejected" && status != "Pending")
            return false;

        var doctor = await _db.Doctors.FirstOrDefaultAsync(d => d.DoctorId == doctorId, cancellationToken);
        if (doctor == null)
            return false;

        doctor.VerificationStatus = status;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<List<AdminConsultationOverviewDto>> GetConsultationsAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Consultations
            .Include(c => c.Patient)
                .ThenInclude(p => p.User)
            .Include(c => c.Doctor)
                .ThenInclude(d => d.User)
            .AsNoTracking()
            .Select(c => new AdminConsultationOverviewDto
            {
                ConsultationId = c.ConsultationId,
                PatientName = c.Patient.User.FullName,
                DoctorName = c.Doctor.User.FullName,
                ScheduledDateTime = c.ScheduledDateTime,
                Status = c.Status,
                Type = c.Type
            })
            .OrderByDescending(c => c.ScheduledDateTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> CancelConsultationAsync(Guid consultationId, CancellationToken cancellationToken = default)
    {
        var consultation = await _db.Consultations.FirstOrDefaultAsync(c => c.ConsultationId == consultationId, cancellationToken);
        if (consultation == null || consultation.Status == "Completed" || consultation.Status == "Cancelled")
            return false;

        var hasNotes = await _db.Set<MediAlert.Models.ConsultationNote>().AnyAsync(n => n.ConsultationId == consultationId, cancellationToken);
        if (hasNotes)
        {
            throw new InvalidOperationException("Cannot cancel a consultation that has clinical notes. Restriction applied.");
        }

        consultation.Status = "Cancelled";
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<List<AdminSubscriptionOverviewDto>> GetSubscriptionsAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Subscriptions
            .Include(s => s.Patient)
                .ThenInclude(p => p.User)
            .AsNoTracking()
            .Select(s => new AdminSubscriptionOverviewDto
            {
                SubscriptionId = s.SubscriptionId,
                PatientName = s.Patient.User.FullName,
                Tier = s.Tier,
                Status = s.Status,
                CancelAtPeriodEnd = s.CancelAtPeriodEnd,
                CreatedAt = s.CreatedAt
            })
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
