using System.Text.Json;

namespace JunoBank.Infrastructure.Email;

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
