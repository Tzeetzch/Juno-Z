using JunoBank.Application.DTOs;
using JunoBank.Application.Interfaces;
using JunoBank.Infrastructure.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace JunoBank.Tests.Services;

public class EmailConfigServiceTests : IDisposable
{
    private readonly string _tempDir;

    public EmailConfigServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"juno-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private EmailConfigService CreateService(Dictionary<string, string?>? configValues = null)
    {
        var defaults = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = $"Data Source={_tempDir}/junobank.db"
        };

        if (configValues != null)
        {
            foreach (var kvp in configValues)
                defaults[kvp.Key] = kvp.Value;
        }

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(defaults)
            .Build();

        var loggerFactory = new Mock<ILoggerFactory>();
        loggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>()))
            .Returns(Mock.Of<ILogger>());

        return new EmailConfigService(config, loggerFactory.Object);
    }

    #region IsConfigured Tests

    [Fact]
    public void IsConfigured_ReturnsFalse_WhenNoHostSet()
    {
        var service = CreateService();

        Assert.False(service.IsConfigured);
    }

    [Fact]
    public void IsConfigured_ReturnsTrue_WhenHostIsSet()
    {
        var service = CreateService(new Dictionary<string, string?>
        {
            ["Email:Host"] = "smtp.gmail.com"
        });

        Assert.True(service.IsConfigured);
    }

    [Fact]
    public void IsConfigured_ReturnsFalse_WhenHostIsEmpty()
    {
        var service = CreateService(new Dictionary<string, string?>
        {
            ["Email:Host"] = ""
        });

        Assert.False(service.IsConfigured);
    }

    #endregion

    #region GetEmailConfig Tests

    [Fact]
    public void GetEmailConfig_ReturnsNull_WhenNotConfigured()
    {
        var service = CreateService();

        var result = service.GetEmailConfig();

        Assert.Null(result);
    }

    [Fact]
    public void GetEmailConfig_ReturnsConfig_WithoutPassword()
    {
        var service = CreateService(new Dictionary<string, string?>
        {
            ["Email:Host"] = "smtp.gmail.com",
            ["Email:Port"] = "465",
            ["Email:Username"] = "user@gmail.com",
            ["Email:Password"] = "secret123",
            ["Email:FromEmail"] = "noreply@example.com"
        });

        var result = service.GetEmailConfig();

        Assert.NotNull(result);
        Assert.Equal("smtp.gmail.com", result.Host);
        Assert.Equal(465, result.Port);
        Assert.Equal("user@gmail.com", result.Username);
        Assert.Equal("", result.Password); // Never exposed
        Assert.Equal("noreply@example.com", result.FromEmail);
    }

    [Fact]
    public void GetEmailConfig_DefaultsPort587()
    {
        var service = CreateService(new Dictionary<string, string?>
        {
            ["Email:Host"] = "smtp.gmail.com",
            ["Email:Username"] = "user@gmail.com",
            ["Email:Password"] = "secret"
        });

        var result = service.GetEmailConfig();

        Assert.NotNull(result);
        Assert.Equal(587, result.Port);
    }

    #endregion

    #region SaveEmailConfigAsync Tests

    [Fact]
    public async Task SaveEmailConfigAsync_WritesConfigFile()
    {
        var service = CreateService();

        await service.SaveEmailConfigAsync(new EmailConfigData
        {
            Host = "smtp.gmail.com",
            Port = 587,
            Username = "user@gmail.com",
            Password = "apppassword",
            FromEmail = "noreply@gmail.com"
        });

        var configPath = Path.Combine(_tempDir, "email-config.json");
        Assert.True(File.Exists(configPath));

        var content = await File.ReadAllTextAsync(configPath);
        Assert.Contains("smtp.gmail.com", content);
        Assert.Contains("apppassword", content);
        Assert.Contains("noreply@gmail.com", content);
    }

    [Fact]
    public async Task SaveEmailConfigAsync_BlankPassword_PreservesExisting()
    {
        // First set up config with an existing password
        var service = CreateService(new Dictionary<string, string?>
        {
            ["Email:Password"] = "original-password"
        });

        await service.SaveEmailConfigAsync(new EmailConfigData
        {
            Host = "smtp.gmail.com",
            Port = 587,
            Username = "user@gmail.com",
            Password = "", // blank = keep existing
            FromEmail = null
        });

        var configPath = Path.Combine(_tempDir, "email-config.json");
        var content = await File.ReadAllTextAsync(configPath);
        Assert.Contains("original-password", content);
    }

    [Fact]
    public async Task SaveEmailConfigAsync_NewPassword_OverwritesExisting()
    {
        var service = CreateService(new Dictionary<string, string?>
        {
            ["Email:Password"] = "old-password"
        });

        await service.SaveEmailConfigAsync(new EmailConfigData
        {
            Host = "smtp.gmail.com",
            Port = 587,
            Username = "user@gmail.com",
            Password = "new-password",
            FromEmail = null
        });

        var configPath = Path.Combine(_tempDir, "email-config.json");
        var content = await File.ReadAllTextAsync(configPath);
        Assert.Contains("new-password", content);
        Assert.DoesNotContain("old-password", content);
    }

    [Fact]
    public async Task SaveEmailConfigAsync_NullFromEmail_DefaultsToUsername()
    {
        var service = CreateService();

        await service.SaveEmailConfigAsync(new EmailConfigData
        {
            Host = "smtp.gmail.com",
            Port = 587,
            Username = "user@gmail.com",
            Password = "pass",
            FromEmail = null
        });

        var configPath = Path.Combine(_tempDir, "email-config.json");
        var content = await File.ReadAllTextAsync(configPath);
        Assert.Contains("user@gmail.com", content);
    }

    #endregion
}
