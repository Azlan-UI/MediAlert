namespace MediAlert.Services.Background;

public sealed class MediAlertBackgroundWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public MediAlertBackgroundWorker(ILogger<MediAlertBackgroundWorker> logger, IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<MediAlertBackgroundWorker>>();
        logger.LogInformation("MediAlert background worker started.");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await ProcessNotificationsAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
        }

        logger.LogInformation("MediAlert background worker stopped.");
    }

    private async Task ProcessNotificationsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MediAlert.Data.ApplicationDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<MediAlert.Services.Notifications.IEmailService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<MediAlertBackgroundWorker>>();

        var now = DateTime.UtcNow;
        var threshold = now.AddMinutes(15);
        var nowTime = TimeOnly.FromDateTime(now);
        var thresholdTime = TimeOnly.FromDateTime(threshold);

        // Fetch upcoming doses
        var query = System.Linq.Queryable.Where(dbContext.DoseSchedules, ds => ds.IsActive);
        
        if (thresholdTime > nowTime)
        {
            query = System.Linq.Queryable.Where(query, ds => ds.ScheduledTime <= thresholdTime && ds.ScheduledTime > nowTime);
        }
        else
        {
            // Crossed midnight
            query = System.Linq.Queryable.Where(query, ds => ds.ScheduledTime > nowTime || ds.ScheduledTime <= thresholdTime);
        }

        var upcomingDoses = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(query, cancellationToken);

        foreach (var dose in upcomingDoses)
        {
            var medication = await dbContext.Medications.FindAsync(new object[] { dose.MedicationId }, cancellationToken);
            if (medication != null)
            {
                var patient = await dbContext.Patients.FindAsync(new object[] { medication.PatientId }, cancellationToken);
                if (patient != null)
                {
                    var user = await dbContext.Users.FindAsync(new object[] { patient.UserId }, cancellationToken);
                    if (user != null && !string.IsNullOrEmpty(user.Email))
                    {
                        var subject = "Upcoming Medication Reminder: " + medication.DrugName;
                        var body = $"<p>Hi {user.FullName},</p><p>This is a reminder to take your medication <strong>{medication.DrugName}</strong> at {dose.ScheduledTime:t}.</p>";
                        await emailService.SendEmailAsync(user.Email, subject, body);
                        logger.LogInformation("Sent reminder for dose {DoseId} to {Email}", dose.DoseScheduleId, user.Email);
                    }
                }
            }
        }
    }
}
