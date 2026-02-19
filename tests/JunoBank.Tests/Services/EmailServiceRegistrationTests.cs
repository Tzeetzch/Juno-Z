using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using JunoBank.Application.Interfaces;
using JunoBank.Infrastructure.Email;

namespace JunoBank.Tests.Services;

public class EmailServiceRegistrationTests
{
    [Fact]
    public void WhenSmtpHostConfigured_RegistersSmtpEmailService()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Email:Host"] = "smtp.example.com",
                ["Email:Port"] = "587",
                ["Email:FromEmail"] = "noreply@example.com"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();

        // Act - register based on config (simulating Program.cs logic)
        var emailHost = configuration.GetValue<string>("Email:Host");
        if (!string.IsNullOrEmpty(emailHost))
        {
            services.AddScoped<IEmailService, SmtpEmailService>();
        }
        else
        {
            services.AddScoped<IEmailService, ConsoleEmailService>();
        }

        var provider = services.BuildServiceProvider();

        // Assert
        var emailService = provider.GetRequiredService<IEmailService>();
        Assert.IsType<SmtpEmailService>(emailService);
    }

    [Fact]
    public void WhenSmtpHostEmpty_RegistersConsoleEmailService()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Email:Host"] = ""
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();

        // Act - register based on config
        var emailHost = configuration.GetValue<string>("Email:Host");
        if (!string.IsNullOrEmpty(emailHost))
        {
            services.AddScoped<IEmailService, SmtpEmailService>();
        }
        else
        {
            services.AddScoped<IEmailService, ConsoleEmailService>();
        }

        var provider = services.BuildServiceProvider();

        // Assert
        var emailService = provider.GetRequiredService<IEmailService>();
        Assert.IsType<ConsoleEmailService>(emailService);
    }

    [Fact]
    public void WhenSmtpHostMissing_RegistersConsoleEmailService()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();

        // Act - register based on config
        var emailHost = configuration.GetValue<string>("Email:Host");
        if (!string.IsNullOrEmpty(emailHost))
        {
            services.AddScoped<IEmailService, SmtpEmailService>();
        }
        else
        {
            services.AddScoped<IEmailService, ConsoleEmailService>();
        }

        var provider = services.BuildServiceProvider();

        // Assert
        var emailService = provider.GetRequiredService<IEmailService>();
        Assert.IsType<ConsoleEmailService>(emailService);
    }
}
