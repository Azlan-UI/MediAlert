using Microsoft.AspNetCore.Identity;

namespace MediAlert.Models;

public class ApplicatioUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsEmailVerified { get; set; }
    public bool IsSuspended { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
