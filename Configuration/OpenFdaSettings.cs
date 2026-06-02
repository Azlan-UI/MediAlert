namespace MediAlert.Configuration;

public class OpenFdaSettings
{
    public string BaseUrl { get; set; } = "https://api.fda.gov";
    public string DrugLabelEndpoint { get; set; } = "/drug/label.json";
    public string? ApiKey { get; set; }
    public int DefaultLimit { get; set; } = 10;
    public int MaxLimit { get; set; } = 25;
    public int TimeoutSeconds { get; set; } = 15;
    public int MaxRetryAttempts { get; set; } = 3;
    public int RetryDelayMilliseconds { get; set; } = 500;
    public string UserAgent { get; set; } = "MediAlert/1.0";
}
