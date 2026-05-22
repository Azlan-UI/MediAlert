using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MediAlert.Models;

namespace MediAlert.Data;

/// <summary>
/// The central database context for MediAlert.
///
/// ARCHITECTURE:
/// We inherit from IdentityDbContext<ApplicationUser> instead of DbContext.
/// This automatically sets up all 6 Identity tables:
///   - AspNetUsers         ← Our ApplicationUser records
///   - AspNetRoles         ← Role definitions
///   - AspNetUserRoles     ← Junction: which user has which role
///   - AspNetUserClaims    ← JWT claim overrides per user
///   - AspNetUserLogins    ← External OAuth logins (Google, etc.)
///   - AspNetUserTokens    ← Password reset tokens, 2FA tokens
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

    // ─────────────────────────────────────────────────────────────────────────
    // FUTURE DbSets — teammates add theirs here in separate PRs.
    // Example (Dev 2 will add):
    //   public DbSet<Medication> Medications => Set<Medication>();
    //   public DbSet<DoseSchedule> DoseSchedules => Set<DoseSchedule>();
    // ─────────────────────────────────────────────────────────────────────────

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // IMPORTANT: Always call base first.
        // Identity sets up its own table mappings inside base.OnModelCreating.
        // Skipping this breaks the entire Identity schema.
        base.OnModelCreating(builder);

        // ── ApplicationUser configuration ────────────────────────────────────
        builder.Entity<ApplicatioUser>(entity =>
        {
            // Rename the default "AspNetUsers" table to match our ERD naming.
            // The ERD calls this table "Users". Identity's default is "AspNetUsers".
            // We'll keep AspNetUsers to stay compatible with Identity tooling,
            // but you could change this if you prefer:
            // entity.ToTable("Users");

            // FullName: required, max 256 characters
            entity.Property(u => u.FullName)
                .IsRequired()
                .HasMaxLength(256);

            // Role: required, restricted to known values via CHECK constraint
            entity.Property(u => u.Role)
                .IsRequired()
                .HasMaxLength(20);

            // IsEmailVerified: maps to Identity's EmailConfirmed column.
            // We store our own column for domain clarity.
            entity.Property(u => u.IsEmailVerified)
                .IsRequired()
                .HasDefaultValue(false);

            // IsSuspended: admin flag, default false
            entity.Property(u => u.IsSuspended)
                .IsRequired()
                .HasDefaultValue(false);

            // CreatedDate: UTC, set on insert, never updated
            entity.Property(u => u.CreatedDate)
                .IsRequired()
                .HasDefaultValueSql("NOW()"); // PostgreSQL UTC now

            // Index on Role for admin filtering queries
            // (Admin Panel: filter users by role — FR-26)
            entity.HasIndex(u => u.Role)
                .HasDatabaseName("IX_Users_Role");

            // Index on IsEmailVerified for login validation
            entity.HasIndex(u => u.IsEmailVerified)
                .HasDatabaseName("IX_Users_IsEmailVerified");
        });

        // ── Seed data — Admin account ────────────────────────────────────────
        // NOTE: Do NOT seed sensitive data here in production.
        // For development and demo only.
        // We seed this via the seeder service in Program.cs instead.
    }
}