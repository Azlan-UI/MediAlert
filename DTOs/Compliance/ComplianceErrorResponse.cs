namespace MediAlert.DTOs.Compliance;

public sealed class ComplianceErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
    public Dictionary<string, string[]>? Errors { get; set; }
}
