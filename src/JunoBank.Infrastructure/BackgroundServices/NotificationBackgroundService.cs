namespace JunoBank.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that checks for due notification emails every 5 minutes.
/// Delegates all business logic to INotificationService for testability.
/// </summary>
public class NotificationBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationBackgroundService> _logger;
    private readonly TimeSpan _checkInterval;

    public NotificationBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<NotificationBackgroundService> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        var intervalSeconds = configuration.GetValue<int>("Notifications:CheckIntervalSeconds", 300);
        _checkInterval = TimeSpan.FromSeconds(intervalSeconds);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Notification Background Service started. Check interval: {Interval}", _checkInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_checkInterval, stoppingToken);
                await ProcessNotificationsAsync();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing notifications");
            }
        }

        _logger.LogInformation("Notification Background Service stopped");
    }

    private async Task ProcessNotificationsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var count = await notificationService.ProcessDueNotificationsAsync();

        if (count > 0)
        {
            _logger.LogInformation("Sent {Count} notification(s)", count);
        }
    }
}
