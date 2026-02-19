namespace JunoBank.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that checks for due allowances every minute.
/// Delegates all business logic to IAllowanceService for testability.
/// </summary>
public class AllowanceBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AllowanceBackgroundService> _logger;
    private readonly TimeSpan _checkInterval;

    public AllowanceBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<AllowanceBackgroundService> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        // Allow configurable interval for testing (default: 1 minute)
        var intervalSeconds = configuration.GetValue<int>("Allowance:CheckIntervalSeconds", 60);
        _checkInterval = TimeSpan.FromSeconds(intervalSeconds);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Allowance Background Service started. Check interval: {Interval}", _checkInterval);

        // Initial check on startup (catches up any missed allowances)
        try
        {
            await ProcessAllowancesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing allowances on startup");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_checkInterval, stoppingToken);
                await ProcessAllowancesAsync();
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing allowances");
                // Continue running despite errors
            }
        }

        _logger.LogInformation("Allowance Background Service stopped");
    }

    private async Task ProcessAllowancesAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var allowanceService = scope.ServiceProvider.GetRequiredService<IAllowanceService>();

        var count = await allowanceService.ProcessDueAllowancesAsync();

        if (count > 0)
        {
            _logger.LogInformation("Processed {Count} allowance(s)", count);
        }
    }
}
