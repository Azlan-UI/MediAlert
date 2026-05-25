using MediAlert.Constants;
using MediAlert.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MediAlert.Data;

/// <summary>
/// The central database context for MediAlert.
///
/// ARCHITECTURE:
/// We inherit from IdentityDbContext<ApplicationUser> instead of DbContext.
/// This automatically sets up all 6 Identity tables:
///   - AspNetUsers         - Our ApplicationUser records
///   - AspNetRoles         - Role definitions
///   - AspNetUserRoles     - Junction: which user has which role
///   - AspNetUserClaims    - JWT claim overrides per user
///   - AspNetUserLogins    - External OAuth logins (Google, etc.)
///   - AspNetUserTokens    - Password reset tokens, 2FA tokens
///
/// As your teammates add modules (Medications, Appointments, etc.),
/// they add DbSet<T> properties HERE. There is only ONE DbContext per app.
///
/// TEAM RULE: Only ONE developer owns migrations at a time.
/// Coordinate before running `dotnet ef migrations add`.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicatioUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Medication> Medications => Set<Medication>();
    public DbSet<DoseSchedule> DoseSchedules => Set<DoseSchedule>();
    public DbSet<IntakeLog> IntakeLogs => Set<IntakeLog>();
    public DbSet<ComplianceReport> ComplianceReports => Set<ComplianceReport>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicatioUser>(entity =>
        {
            entity.Property(u => u.FullName)
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(u => u.Role)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(u => u.IsEmailVerified)
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(u => u.IsSuspended)
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(u => u.CreatedDate)
                .IsRequired()
                .HasDefaultValueSql("NOW()");

            entity.HasIndex(u => u.Role)
                .HasDatabaseName("IX_Users_Role");

            entity.HasIndex(u => u.IsEmailVerified)
                .HasDatabaseName("IX_Users_IsEmailVerified");
        });

        builder.Entity<Patient>(entity =>
        {
            entity.ToTable("Patients", table =>
            {
                table.HasCheckConstraint(
                    "CK_Patients_ComplianceStreakDays_NonNegative",
                    "\"ComplianceStreakDays\" >= 0");
            });

            entity.HasKey(p => p.PatientId);

            entity.Property(p => p.UserId)
                .IsRequired();

            entity.Property(p => p.Gender)
                .HasMaxLength(30);

            entity.Property(p => p.PhoneNumber)
                .HasMaxLength(20);

            entity.Property(p => p.ComplianceStreakDays)
                .IsRequired()
                .HasDefaultValue(0);

            entity.Property(p => p.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW()");

            entity.HasIndex(p => p.UserId)
                .IsUnique()
                .HasDatabaseName("IX_Patients_UserId");

            entity.HasOne(p => p.User)
                .WithOne()
                .HasForeignKey<Patient>(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Medication>(entity =>
        {
            entity.ToTable("Medications", table =>
            {
                table.HasCheckConstraint(
                    "CK_Medications_FrequencyPerDay_Range",
                    "\"FrequencyPerDay\" BETWEEN 1 AND 24");

                table.HasCheckConstraint(
                    "CK_Medications_DateRange",
                    "\"EndDate\" IS NULL OR \"EndDate\" >= \"StartDate\"");
            });

            entity.HasKey(m => m.MedicationId);

            entity.Property(m => m.DrugName)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(m => m.DosageStrength)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(m => m.DosageForm)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(m => m.PrescribingPhysician)
                .HasMaxLength(150);

            entity.Property(m => m.PharmacyName)
                .HasMaxLength(150);

            entity.Property(m => m.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            entity.Property(m => m.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW()");

            entity.HasIndex(m => new { m.PatientId, m.IsActive })
                .HasDatabaseName("IX_Medications_PatientId_IsActive");

            entity.HasOne(m => m.Patient)
                .WithMany(p => p.Medications)
                .HasForeignKey(m => m.PatientId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<DoseSchedule>(entity =>
        {
            entity.ToTable("DoseSchedules", table =>
            {
                table.HasCheckConstraint(
                    "CK_DoseSchedules_DayOfWeek_Range",
                    "\"DayOfWeek\" IS NULL OR \"DayOfWeek\" BETWEEN 0 AND 6");
            });

            entity.HasKey(ds => ds.DoseScheduleId);

            entity.Property(ds => ds.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            entity.Property(ds => ds.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW()");

            entity.HasIndex(ds => new { ds.MedicationId, ds.IsActive })
                .HasDatabaseName("IX_DoseSchedules_MedicationId_IsActive");

            entity.HasOne(ds => ds.Medication)
                .WithMany(m => m.DoseSchedules)
                .HasForeignKey(ds => ds.MedicationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<IntakeLog>(entity =>
        {
            entity.ToTable("IntakeLogs", table =>
            {
                table.HasCheckConstraint(
                    "CK_IntakeLogs_Status",
                    $"\"Status\" IN ('{IntakeStatuses.Taken}', '{IntakeStatuses.Skipped}', '{IntakeStatuses.Missed}', '{IntakeStatuses.Delayed}')");
            });

            entity.HasKey(il => il.IntakeLogId);

            entity.Property(il => il.Status)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(il => il.SkippedReason)
                .HasMaxLength(250);

            entity.Property(il => il.Notes)
                .HasMaxLength(500);

            entity.Property(il => il.LoggedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW()");

            entity.HasIndex(il => new { il.DoseScheduleId, il.ScheduledDate })
                .IsUnique()
                .HasDatabaseName("UX_IntakeLogs_DoseScheduleId_ScheduledDate");

            entity.HasIndex(il => new { il.PatientId, il.ScheduledDate })
                .HasDatabaseName("IX_IntakeLogs_PatientId_ScheduledDate");

            entity.HasIndex(il => il.Status)
                .HasDatabaseName("IX_IntakeLogs_Status");

            entity.HasOne(il => il.Patient)
                .WithMany(p => p.IntakeLogs)
                .HasForeignKey(il => il.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(il => il.DoseSchedule)
                .WithMany(ds => ds.IntakeLogs)
                .HasForeignKey(il => il.DoseScheduleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ComplianceReport>(entity =>
        {
            entity.ToTable("ComplianceReports", table =>
            {
                table.HasCheckConstraint(
                    "CK_ComplianceReports_DateRange",
                    "\"PeriodEndDate\" >= \"PeriodStartDate\"");

                table.HasCheckConstraint(
                    "CK_ComplianceReports_TotalScheduledDoses_NonNegative",
                    "\"TotalScheduledDoses\" >= 0");

                table.HasCheckConstraint(
                    "CK_ComplianceReports_DoseCounts_NonNegative",
                    "\"TakenDoses\" >= 0 AND \"SkippedDoses\" >= 0 AND \"MissedDoses\" >= 0 AND \"DelayedDoses\" >= 0");

                table.HasCheckConstraint(
                    "CK_ComplianceReports_DoseCounts_NotOverTotal",
                    "(\"TakenDoses\" + \"SkippedDoses\" + \"MissedDoses\" + \"DelayedDoses\") <= \"TotalScheduledDoses\"");

                table.HasCheckConstraint(
                    "CK_ComplianceReports_OverallCompliancePercentage_Range",
                    "\"OverallCompliancePercentage\" BETWEEN 0 AND 100");
            });

            entity.HasKey(cr => cr.ComplianceReportId);

            entity.Property(cr => cr.OverallCompliancePercentage)
                .HasPrecision(5, 2);

            entity.Property(cr => cr.Recommendations)
                .HasMaxLength(1000);

            entity.Property(cr => cr.GeneratedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW()");

            entity.HasIndex(cr => new { cr.PatientId, cr.PeriodStartDate, cr.PeriodEndDate })
                .IsUnique()
                .HasDatabaseName("UX_ComplianceReports_PatientId_Period");

            entity.HasOne(cr => cr.Patient)
                .WithMany(p => p.ComplianceReports)
                .HasForeignKey(cr => cr.PatientId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
