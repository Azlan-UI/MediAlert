using System.ComponentModel.DataAnnotations;

namespace MediAlert.DTOs.HealthProfile;

public class HealthConditionDto
{
    public Guid ConditionId { get; set; }
    public string ConditionName { get; set; } = string.Empty;
    public DateOnly? DiagnosedDate { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateHealthConditionRequest
{
    [Required]
    [MaxLength(150)]
    public string ConditionName { get; set; } = string.Empty;

    public DateOnly? DiagnosedDate { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }
}

public class UpdateHealthConditionRequest
{
    [Required]
    [MaxLength(150)]
    public string ConditionName { get; set; } = string.Empty;

    public DateOnly? DiagnosedDate { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }
}
