namespace MediAlert.DTOs.OpenFda;

public sealed class OpenFdaDrugSearchItem
{
    public string? LabelId { get; set; }
    public string? SetId { get; set; }
    public string? EffectiveTime { get; set; }
    public List<string> BrandNames { get; set; } = [];
    public List<string> GenericNames { get; set; } = [];
    public List<string> ManufacturerNames { get; set; } = [];
    public List<string> ProductTypes { get; set; } = [];
    public List<string> Routes { get; set; } = [];
    public List<string> SubstanceNames { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
    public List<string> BoxedWarnings { get; set; } = [];
    public List<string> Contraindications { get; set; } = [];
    public List<string> DrugInteractions { get; set; } = [];
}
