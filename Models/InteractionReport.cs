using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MediAlert.Models;

public class InteractionReport
{
    [Key]
    public Guid ReportId { get; set; } = Guid.NewGuid();

    [Required]
    [ForeignKey("Patient")]
    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string QueryDrugName { get; set; } = string.Empty;

    [Required]
    public string ExistingDrugNames { get; set; } = string.Empty; // Comma separated

    [Required]
    [MaxLength(50)]
    public string SeverityLevel { get; set; } = string.Empty; // Minor, Moderate, Major, Contraindicated

    [Required]
    public string ExplanationText { get; set; } = string.Empty;

    public bool IsSaved { get; set; } = true;

    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
