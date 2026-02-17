using System.Text.Json;

namespace JunoBank.Web.Services;

/// <summary>
/// Data for SMTP email configuration.
/// Shared between setup wizard and email settings dialog.
/// </summary>
public class EmailConfigData
{
    public required string Host { get; set; }
    public int Port { get; set; } = 587;
    public required string Username { get; set; }
    public required string Password { get; set; }
    public string? FromEmail { get; set; }
}

/// <summary>
/// Service for reading, writing, and testing email (SMTP) configuration.
/// Config is stored in Data/email-config.json and loaded with reloadOnChange.
/// </summary>
public interface IEmailConfigService
{
    /// <summary>
    /// Whether SMTP email is currently configured (Email:Host is set).
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Read current email config from IConfiguration.
    /// Returns null if not configured. Password is always omitted.
    /// </summary>
    EmailConfigData? GetEmailConfig();

    /// <summary>
    /// Write email config to Data/email-config.json.
    /// If password is empty/null, preserves the existing password from config.
    /// </summary>
    Task SaveEmailConfigAsync(EmailConfigData config);

    /// <summary>
    /// Send a test email using the provided candidate config (not the saved config).
    /// Returns true on success.
    /// </summary>
    Task<bool> SendTestEmailAsync(EmailConfigData config, string targetEmail);
}

public class EmailConfigService : IEmailConfigService
{
    private readonly IConfiguration _configuration;
    private readonly ILoggerFactory _loggerFactory;

    public EmailConfigService(IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        _configuration = configuration;
        _loggerFactory = loggerFactory;
    }

    public bool IsConfigured =>
        !string.IsNullOrEmpty(_configuration.GetValue<string>("Email:Host"));

    public EmailConfigData? GetEmailConfig()
    {
        var section = _configuration.GetSection("Email");
        var host = section["Host"];

        if (string.IsNullOrEmpty(host))
            return null;

        return new EmailConfigData
        {
            Host = host,
            Port = section.GetValue<int>("Port", 587),
            Username = section["Username"] ?? "",
            Password = "", // Never expose stored password
            FromEmail = section["FromEmail"]
        };
    }

    public async Task SaveEmailConfigAsync(EmailConfigData config)
    {
        // If password is blank, preserve the existing one from config
        var password = config.Password;
        if (string.IsNullOrEmpty(password))
        {
            password = _configuration["Email:Password"] ?? "";
        }

        var configPath = GetConfigPath();
        Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);

        var configObj = new
        {
            Email = new
            {
                Host = config.Host,
                Port = config.Port,
                Username = config.Username,
                Password = password,
                FromEmail = config.FromEmail ?? config.Username,
                UseSsl = true
            }
        };

        var json = JsonSerializer.Serialize(configObj, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(configPath, json);
    }

    public async Task<bool> SendTestEmailAsync(EmailConfigData config, string targetEmail)
    {
        // Build a temporary config for SmtpEmailService
        var configData = new Dictionary<string, string?>
        {
            ["Email:Host"] = config.Host,
            ["Email:Port"] = config.Port.ToString(),
            ["Email:Username"] = config.Username,
            ["Email:Password"] = config.Password,
            ["Email:FromEmail"] = string.IsNullOrWhiteSpace(config.FromEmail) ? config.Username : config.FromEmail
        };

        var tempConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var logger = _loggerFactory.CreateLogger<SmtpEmailService>();
        var smtp = new SmtpEmailService(tempConfig, logger);

        return await smtp.SendEmailAsync(
            targetEmail,
            "Juno Bank - Test Email",
            "<h2>It works!</h2><p>Your Juno Bank email is configured correctly. Password reset emails will be sent to this address.</p>");
    }

    private string GetConfigPath()
    {
        var connStr = _configuration.GetConnectionString("DefaultConnection") ?? "Data Source=Data/junobank.db";
        var dbPath = connStr.Replace("Data Source=", "");
        var dataDir = Path.GetDirectoryName(dbPath) ?? "Data";
        return Path.Combine(dataDir, "email-config.json");
    }
}
