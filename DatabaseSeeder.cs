using MediAlert.Constants;
using MediAlert.Models;
using Microsoft.AspNetCore.Identity;

namespace MediAlert;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicatioUser> userManager,
        ILogger logger)
    {
        var roles = new[] { UserRoles.Patient, UserRoles.Caregiver, UserRoles.Doctor, UserRoles.Admin };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                logger.LogInformation("Created role: {Role}", role);
            }
        }
    }
}
