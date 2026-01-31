using MailKit.Net.Smtp;
using MimeKit;

namespace JunoBank.Web.Services;

/// <summary>
/// SMTP email service using MailKit.
/// Reads configuration from Email section in appsettings.json.
/// </summary>
public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string htmlBody)
    {
        try
        {
            var emailSection = _configuration.GetSection("Email");
            var host = emailSection["Host"] ?? throw new InvalidOperationException("Email:Host not configured");
            var port = emailSection.GetValue<int>("Port", 587);
            var username = emailSection["Username"];
            var password = emailSection["Password"];
            var fromEmail = emailSection["FromEmail"] ?? username ?? throw new InvalidOperationException("Email:FromEmail not configured");
            var fromName = emailSection["FromName"] ?? "Juno Bank";
            var useSsl = emailSection.GetValue<bool>("UseSsl", true);

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromEmail));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = htmlBody
            };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            
            await client.ConnectAsync(host, port, useSsl);
            
            // Only authenticate if credentials are provided
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                await client.AuthenticateAsync(username, password);
            }

            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Email sent to {To}: {Subject}", to, subject);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}: {Subject}", to, subject);
            return false;
        }
    }
}
