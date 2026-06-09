using MediAlert.Constants;
using MediAlert.Models;
using Microsoft.AspNetCore.Identity;
using ApplicationUser = MediAlert.Models.ApplicatioUser;

namespace MediAlert;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager,
        ILogger logger)
    {
        // ─────────────────────────────────────────────
        // 1. CREATE ROLES
        // ─────────────────────────────────────────────
        var roles = new[]
        {
            UserRoles.Patient,
            UserRoles.Caregiver,
            UserRoles.Doctor,
            UserRoles.Admin
        };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                logger.LogInformation("Created role: {Role}", role);
            }
        }

        // ─────────────────────────────────────────────
        // 2. CREATE DEFAULT ADMIN USER (FOR TESTING LOGIN)
        // ─────────────────────────────────────────────
        string adminEmail = "admin@medialert.com";
        string adminPassword = "Admin@12345"; // must satisfy Identity rules

        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);

        if (existingAdmin == null)
        {
            var adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "System Admin",
                Role = UserRoles.Admin,
                EmailConfirmed = true,
                IsEmailVerified = true,
                IsSuspended = false,
                CreatedDate = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, UserRoles.Admin);
                logger.LogInformation("Default admin user created: {Email}", adminEmail);
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    logger.LogError("Admin creation error: {Error}", error.Description);
                }
            }
        }
        else
        {
            // Force verify email and reset password in case it was created incorrectly before
            existingAdmin.IsEmailVerified = true;
            existingAdmin.EmailConfirmed = true;
            await userManager.UpdateAsync(existingAdmin);
            
            var token = await userManager.GeneratePasswordResetTokenAsync(existingAdmin);
            await userManager.ResetPasswordAsync(existingAdmin, token, adminPassword);
            
            if (!await userManager.IsInRoleAsync(existingAdmin, UserRoles.Admin))
            {
                await userManager.AddToRoleAsync(existingAdmin, UserRoles.Admin);
            }

            logger.LogInformation("Admin user already existed. Forced password reset and verification for: {Email}", adminEmail);
        }
    }
}