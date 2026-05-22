
using Microsoft.AspNetCore.Identity;
using MediAlert.Constants;
using MediAlert.Models;
using ApplicationUser = MediAlert.Models.ApplicatioUser;
namespace MediAlert.Data;


/// <summary>
/// Seeds the database with required initial data on application startup.
///
/// WHY DO WE NEED A SEEDER?
/// Identity's role-based authorization ([Authorize(Roles = "Admin")]) requires
/// that roles exist in the AspNetRoles table. Without seeded roles, calling
/// AddToRoleAsync("Admin") throws a runtime exception.
///
/// We also seed a default Admin account so you can log in and test
/// admin features on a fresh database.
///
/// HOW IT RUNS:
/// This is called from Program.cs AFTER the app is built but BEFORE
/// it starts listening for requests.
///
/// TEAM IMPACT:
/// This runs on every startup but is idempotent — it checks before inserting.
/// Safe for all teammates to run. Do NOT remove idempotency checks.
/// </summary>
public static class DatabaseSeeder
{
    /// <summary>
    /// Seeds roles and the default admin user.
    ///
    /// SECURITY NOTE:
    /// The default admin credentials here are for development ONLY.
    /// In production, change the password immediately or provision admin
    /// accounts through a secure out-of-band process.
    /// </summary>
    public static async Task SeedAsync(
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager,
        ILogger logger)
    {
        await SeedRolesAsync(roleManager, logger);
        await SeedDefaultAdminAsync(userManager, logger);
    }

    /// <summary>
    /// Ensures all four MediAlert roles exist in AspNetRoles.
    ///
    /// Must run before any user registration that assigns roles.
    /// Safe to run multiple times — checks existence before creating.
    /// </summary>
    private static async Task SeedRolesAsync(
        RoleManager<IdentityRole> roleManager,
        ILogger logger)
    {
        foreach (var roleName in UserRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(roleName));

                if (result.Succeeded)
                {
                    logger.LogInformation("Seeded role: {Role}", roleName);
                }
                else
                {
                    logger.LogError(
                        "Failed to seed role {Role}: {@Errors}",
                        roleName,
                        result.Errors.Select(e => e.Description));
                }
            }
        }
    }

    /// <summary>
    /// Creates a default Admin account for development.
    ///
    /// Email: admin@medialert.dev
    /// Password: Admin@123456
    ///
    /// Change these in appsettings.Development.json for your environment.
    /// </summary>
    private static async Task SeedDefaultAdminAsync(
        UserManager<ApplicationUser> userManager,
        ILogger logger)
    {
        const string adminEmail = "admin@medialert.dev";
        const string adminPassword = "Admin@123456";

        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
        if (existingAdmin is not null)
        {
            // Admin already seeded — nothing to do.
            return;
        }

        var admin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FullName = "MediAlert Admin",
            Role = UserRoles.Admin,
            IsEmailVerified = true,
            EmailConfirmed = true,
            IsSuspended = false,
            CreatedDate = DateTime.UtcNow,
        };

        var createResult = await userManager.CreateAsync(admin, adminPassword);

        if (!createResult.Succeeded)
        {
            logger.LogError(
                "Failed to seed admin account: {@Errors}",
                createResult.Errors.Select(e => e.Description));
            return;
        }

        var roleResult = await userManager.AddToRoleAsync(admin, UserRoles.Admin);

        if (roleResult.Succeeded)
        {
            logger.LogInformation(
                "Default admin account seeded: {Email}", adminEmail);
        }
    }
}
