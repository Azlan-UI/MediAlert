namespace MediAlert.DTOs.OpenFda;

public sealed class OpenFdaDrugSearchRequest
{
    public string Query { get; set; } = string.Empty;
    public int? Limit { get; set; }
    public int? Skip { get; set; }
}
