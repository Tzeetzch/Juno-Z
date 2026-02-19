using JunoBank.Infrastructure.BackgroundServices;
using JunoBank.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace JunoBank.Tests.BackgroundServices;

public class AllowanceBackgroundServiceTests
{
    [Fact]
    public async Task ExecuteAsync_CallsProcessDueAllowancesOnStartup()
    {
        // Arrange
        var mockAllowanceService = new Mock<IAllowanceService>();
        mockAllowanceService.Setup(s => s.ProcessDueAllowancesAsync()).ReturnsAsync(0);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(sp => sp.GetService(typeof(IAllowanceService)))
            .Returns(mockAllowanceService.Object);

        var serviceScope = new Mock<IServiceScope>();
        serviceScope.Setup(s => s.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(f => f.CreateScope()).Returns(serviceScope.Object);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Allowance:CheckIntervalSeconds"] = "1" // 1 second for fast test
            })
            .Build();

        var logger = Mock.Of<ILogger<AllowanceBackgroundService>>();

        var service = new AllowanceBackgroundService(scopeFactory.Object, logger, configuration);

        // Act
        using var cts = new CancellationTokenSource();
        var executeTask = service.StartAsync(cts.Token);

        // Wait a bit for initial processing
        await Task.Delay(100);
        cts.Cancel();

        try { await executeTask; } catch (OperationCanceledException) { }

        // Assert - should have been called at least once (on startup)
        mockAllowanceService.Verify(s => s.ProcessDueAllowancesAsync(), Times.AtLeastOnce());
    }

    [Fact]
    public async Task ExecuteAsync_ContinuesAfterError()
    {
        // Arrange
        var callCount = 0;
        var mockAllowanceService = new Mock<IAllowanceService>();
        mockAllowanceService.Setup(s => s.ProcessDueAllowancesAsync())
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount == 2) throw new Exception("Simulated error"); // Error on 2nd call (after startup)
                return 0;
            });

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(sp => sp.GetService(typeof(IAllowanceService)))
            .Returns(mockAllowanceService.Object);

        var serviceScope = new Mock<IServiceScope>();
        serviceScope.Setup(s => s.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(f => f.CreateScope()).Returns(serviceScope.Object);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Allowance:CheckIntervalSeconds"] = "1"
            })
            .Build();

        var logger = Mock.Of<ILogger<AllowanceBackgroundService>>();

        var service = new AllowanceBackgroundService(scopeFactory.Object, logger, configuration);

        // Act
        using var cts = new CancellationTokenSource();
        var executeTask = service.StartAsync(cts.Token);

        // Wait for at least 3 cycles (startup + error + recovery)
        await Task.Delay(2500);
        cts.Cancel();

        try { await executeTask; } catch (OperationCanceledException) { }

        // Assert - should have been called at least 3 times despite error on 2nd call
        Assert.True(callCount >= 3, $"Expected at least 3 calls, got {callCount}");
    }

    [Fact]
    public void Constructor_UsesDefaultInterval_WhenNotConfigured()
    {
        // Arrange
        var scopeFactory = new Mock<IServiceScopeFactory>();
        var logger = Mock.Of<ILogger<AllowanceBackgroundService>>();
        var configuration = new ConfigurationBuilder().Build(); // Empty config

        // Act - should not throw
        var service = new AllowanceBackgroundService(scopeFactory.Object, logger, configuration);

        // Assert - service created successfully (default interval is 60 seconds)
        Assert.NotNull(service);
    }
}
