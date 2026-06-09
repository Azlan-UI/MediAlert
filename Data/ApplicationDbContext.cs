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
    public DbSet<InteractionReport> InteractionReports => Set<InteractionReport>();
    public DbSet<ComplianceReport> ComplianceReports => Set<ComplianceReport>();
    public DbSet<Caregiver> Caregivers => Set<Caregiver>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<Admin> Admins => Set<Admin>();
    public DbSet<CaregiverPatientLink> CaregiverPatientLinks => Set<CaregiverPatientLink>();
    public DbSet<DoctorAvailability> DoctorAvailabilities => Set<DoctorAvailability>();
    public DbSet<PatientDoctorLink> PatientDoctorLinks => Set<PatientDoctorLink>();
    public DbSet<Consultation> Consultations => Set<Consultation>();
    public DbSet<ConsultationNote> ConsultationNotes => Set<ConsultationNote>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<ProcessedStripeEvent> ProcessedStripeEvents => Set<ProcessedStripeEvent>();
    public DbSet<BillingAuditLog> BillingAuditLogs => Set<BillingAuditLog>();
    public DbSet<HealthCondition> HealthConditions => Set<HealthCondition>();
    public DbSet<RefillReminder> RefillReminders => Set<RefillReminder>();
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

        builder.Entity<HealthCondition>(entity =>
        {
            entity.HasKey(hc => hc.ConditionId);

            entity.Property(hc => hc.ConditionName)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(hc => hc.Notes)
                .HasMaxLength(1000);

            entity.Property(hc => hc.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW()");

            entity.HasIndex(hc => hc.PatientId)
                .HasDatabaseName("IX_HealthConditions_PatientId");

            entity.HasOne(hc => hc.Patient)
                .WithMany(p => p.HealthConditions)
                .HasForeignKey(hc => hc.PatientId)
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
        builder.Entity<Caregiver>(entity =>
        {
            entity.HasKey(c => c.CaregiverId);
            entity.Property(c => c.UserId).IsRequired();
            entity.Property(c => c.PhoneNumber).HasMaxLength(20);
            entity.Property(c => c.CreatedAt).IsRequired().HasDefaultValueSql("NOW()");
            
            entity.HasIndex(c => c.UserId).IsUnique().HasDatabaseName("IX_Caregivers_UserId");
            
            entity.HasOne(c => c.User).WithOne().HasForeignKey<Caregiver>(c => c.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Doctor>(entity =>
        {
            entity.HasKey(d => d.DoctorId);
            entity.Property(d => d.UserId).IsRequired();
            entity.Property(d => d.LicenseNumber).IsRequired().HasMaxLength(50);
            entity.Property(d => d.Specialization).IsRequired().HasMaxLength(100);
            entity.Property(d => d.Qualifications).IsRequired().HasMaxLength(200);
            entity.Property(d => d.VerificationStatus).IsRequired().HasMaxLength(20);
            entity.Property(d => d.CreatedAt).IsRequired().HasDefaultValueSql("NOW()");
            
            entity.HasIndex(d => d.UserId).IsUnique().HasDatabaseName("IX_Doctors_UserId");
            entity.HasIndex(d => d.LicenseNumber).IsUnique().HasDatabaseName("UX_Doctors_LicenseNumber");
            
            entity.HasOne(d => d.User).WithOne().HasForeignKey<Doctor>(d => d.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Admin>(entity =>
        {
            entity.HasKey(a => a.AdminId);
            entity.Property(a => a.UserId).IsRequired();
            entity.Property(a => a.CreatedAt).IsRequired().HasDefaultValueSql("NOW()");
            
            entity.HasIndex(a => a.UserId).IsUnique().HasDatabaseName("IX_Admins_UserId");
            
            entity.HasOne(a => a.User).WithOne().HasForeignKey<Admin>(a => a.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<CaregiverPatientLink>(entity =>
        {
            entity.HasKey(l => l.CaregiverPatientLinkId);
            entity.Property(l => l.Status).IsRequired().HasMaxLength(20);
            entity.Property(l => l.CreatedAt).IsRequired().HasDefaultValueSql("NOW()");
            
            entity.HasIndex(l => new { l.CaregiverId, l.PatientId }).IsUnique().HasDatabaseName("UX_CaregiverPatientLinks_Caregiver_Patient");
            
            entity.HasOne(l => l.Caregiver).WithMany(c => c.LinkedPatients).HasForeignKey(l => l.CaregiverId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(l => l.Patient).WithMany(p => p.Caregivers).HasForeignKey(l => l.PatientId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<DoctorAvailability>(entity =>
        {
            entity.HasKey(a => a.DoctorAvailabilityId);
            entity.Property(a => a.CreatedAt).IsRequired().HasDefaultValueSql("NOW()");
            
            entity.HasIndex(a => new { a.DoctorId, a.DayOfWeek }).HasDatabaseName("IX_DoctorAvailabilities_Doctor_DayOfWeek");
            
            entity.HasOne(a => a.Doctor).WithMany(d => d.Availabilities).HasForeignKey(a => a.DoctorId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PatientDoctorLink>(entity =>
        {
            entity.HasKey(l => l.PatientDoctorLinkId);
            entity.Property(l => l.Status).IsRequired().HasMaxLength(20);
            entity.Property(l => l.CreatedAt).IsRequired().HasDefaultValueSql("NOW()");
            
            entity.HasIndex(l => new { l.PatientId, l.DoctorId }).IsUnique().HasDatabaseName("UX_PatientDoctorLinks_Patient_Doctor");
            
            entity.HasOne(l => l.Patient).WithMany(p => p.Doctors).HasForeignKey(l => l.PatientId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(l => l.Doctor).WithMany(d => d.LinkedPatients).HasForeignKey(l => l.DoctorId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Consultation>(entity =>
        {
            entity.HasKey(c => c.ConsultationId);
            entity.Property(c => c.Type).IsRequired().HasMaxLength(50);
            entity.Property(c => c.Status).IsRequired().HasMaxLength(20);
            entity.Property(c => c.ZoomMeetingUrl).HasMaxLength(500);
            entity.Property(c => c.CreatedAt).IsRequired().HasDefaultValueSql("NOW()");
            
            entity.HasIndex(c => new { c.DoctorId, c.ScheduledDateTime }).HasDatabaseName("IX_Consultations_Doctor_ScheduledTime");
            
            entity.HasOne(c => c.Patient).WithMany(p => p.Consultations).HasForeignKey(c => c.PatientId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(c => c.Doctor).WithMany(d => d.Consultations).HasForeignKey(c => c.DoctorId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ConsultationNote>(entity =>
        {
            entity.HasKey(n => n.ConsultationNoteId);
            entity.Property(n => n.ClinicalNotes).IsRequired();
            entity.Property(n => n.CreatedAt).IsRequired().HasDefaultValueSql("NOW()");
            
            entity.HasIndex(n => n.ConsultationId).IsUnique().HasDatabaseName("IX_ConsultationNotes_ConsultationId");
            
            entity.HasOne(n => n.Consultation).WithOne(c => c.ConsultationNote).HasForeignKey<ConsultationNote>(n => n.ConsultationId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Subscription>(entity =>
        {
            entity.HasKey(s => s.SubscriptionId);
            entity.Property(s => s.Tier).IsRequired().HasMaxLength(50);
            entity.Property(s => s.Status).IsRequired().HasMaxLength(20);
            entity.Property(s => s.StripeCustomerId).HasMaxLength(150);
            entity.Property(s => s.StripeSubscriptionId).HasMaxLength(150);
            entity.Property(s => s.StripePriceId).HasMaxLength(150);
            entity.Property(s => s.CancelAtPeriodEnd).IsRequired().HasDefaultValue(false);
            entity.Property(s => s.CreatedAt).IsRequired().HasDefaultValueSql("NOW()");
            
            entity.HasIndex(s => s.PatientId).HasDatabaseName("IX_Subscriptions_PatientId");
            entity.HasIndex(s => s.StripeCustomerId).HasDatabaseName("IX_Subscriptions_StripeCustomerId");
            entity.HasIndex(s => s.StripeSubscriptionId).IsUnique().HasDatabaseName("UX_Subscriptions_StripeSubscriptionId");
            
            entity.HasOne(s => s.Patient).WithMany(p => p.Subscriptions).HasForeignKey(s => s.PatientId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Invoice>(entity =>
        {
            entity.HasKey(i => i.InvoiceId);
            entity.Property(i => i.StripeInvoiceId).HasMaxLength(150);
            entity.Property(i => i.Currency).IsRequired().HasMaxLength(10);
            entity.Property(i => i.Status).IsRequired().HasMaxLength(20);
            entity.Property(i => i.HostedInvoiceUrl).HasMaxLength(500);
            entity.Property(i => i.InvoicePdfUrl).HasMaxLength(500);
            entity.Property(i => i.AttemptCount).IsRequired().HasDefaultValue(0);
            entity.Property(i => i.CreatedAt).IsRequired().HasDefaultValueSql("NOW()");
            
            entity.HasIndex(i => i.SubscriptionId).HasDatabaseName("IX_Invoices_SubscriptionId");
            entity.HasIndex(i => i.StripeInvoiceId).IsUnique().HasDatabaseName("UX_Invoices_StripeInvoiceId");
            
            entity.HasOne(i => i.Subscription).WithMany(s => s.Invoices).HasForeignKey(i => i.SubscriptionId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ProcessedStripeEvent>(entity =>
        {
            entity.HasKey(e => e.EventId);
            entity.Property(e => e.EventId).HasMaxLength(255);
            entity.Property(e => e.ProcessedAt).IsRequired().HasDefaultValueSql("NOW()");
        });

        builder.Entity<BillingAuditLog>(entity =>
        {
            entity.HasKey(a => a.AuditId);
            entity.Property(a => a.Action).IsRequired().HasMaxLength(100);
            entity.Property(a => a.Details).HasMaxLength(1000);
            entity.Property(a => a.CreatedAt).IsRequired().HasDefaultValueSql("NOW()");

            entity.HasIndex(a => a.PatientId).HasDatabaseName("IX_BillingAuditLogs_PatientId");

            entity.HasOne(a => a.Patient).WithMany().HasForeignKey(a => a.PatientId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<RefillReminder>(entity =>
        {
            entity.HasKey(r => r.ReminderId);
            entity.Property(r => r.Status).IsRequired().HasMaxLength(20);
            entity.Property(r => r.CreatedAt).IsRequired().HasDefaultValueSql("NOW()");

            entity.HasIndex(r => new { r.PatientId, r.Status }).HasDatabaseName("IX_RefillReminders_Patient_Status");

            entity.HasOne(r => r.Patient).WithMany().HasForeignKey(r => r.PatientId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(r => r.Medication).WithMany().HasForeignKey(r => r.MedicationId).OnDelete(DeleteBehavior.Restrict);
        });

    }
}
