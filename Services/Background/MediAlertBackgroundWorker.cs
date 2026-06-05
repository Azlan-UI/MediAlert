namespace MediAlert.Services.Background;

public sealed class MediAlertBackgroundWorker : BackgroundService
{
    private readonly ILogger<MediAlertBackgroundWorker> _logger;

    public MediAlertBackgroundWorker(ILogger<MediAlertBackgroundWorker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MediAlert background worker started.");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
        }

        _logger.LogInformation("MediAlert background worker stopped.");
    }
}
