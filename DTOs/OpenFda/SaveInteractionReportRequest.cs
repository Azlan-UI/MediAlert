using System.ComponentModel.DataAnnotations;

namespace MediAlert.DTOs.OpenFda;

public class SaveInteractionReportRequest
{
    [Required]
    public string QueryDrugName { get; set; } = string.Empty;

    [Required]
    public string ExistingDrugNames { get; set; } = string.Empty;

    [Required]
    public string SeverityLevel { get; set; } = string.Empty;

    [Required]
    public string ExplanationText { get; set; } = string.Empty;
}
