namespace MediAlert.DTOs.OpenFda;

public sealed class OpenFdaDrugSearchResponse
{
    public string Query { get; set; } = string.Empty;
    public int TotalResults { get; set; }
    public string Source { get; set; } = "OpenFDA Drug Label API";
    public List<OpenFdaDrugSearchItem> Results { get; set; } = [];
}
