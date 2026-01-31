namespace JunoBank.Web.Services;

/// <summary>
/// Service for sending emails.
/// Implementations: SmtpEmailService (production), ConsoleEmailService (dev/test)
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Send an email asynchronously.
    /// </summary>
    /// <param name="to">Recipient email address</param>
    /// <param name="subject">Email subject</param>
    /// <param name="htmlBody">HTML content of the email</param>
    /// <returns>True if sent successfully, false otherwise</returns>
    Task<bool> SendEmailAsync(string to, string subject, string htmlBody);
}
