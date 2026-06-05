namespace MediAlert.Models;

public class Admin
{
    public Guid AdminId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ApplicatioUser User { get; set; } = null!;
}
