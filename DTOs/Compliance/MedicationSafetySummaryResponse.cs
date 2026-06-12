namespace MediAlert.DTOs.Compliance;

public sealed class MedicationSafetySummaryResponse
{
    public Guid MedicationId { get; set; }
    public string DrugName { get; set; } = string.Empty;
    public bool RetrievedFromOpenFda { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> BrandNames { get; set; } = [];
    public List<string> GenericNames { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
    public List<string> BoxedWarnings { get; set; } = [];
    public List<string> Contraindications { get; set; } = [];
    public List<string> DrugInteractions { get; set; } = [];
}
