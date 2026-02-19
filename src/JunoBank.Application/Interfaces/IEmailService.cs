namespace JunoBank.Application.Interfaces;

/// <summary>
/// Email sending service abstraction.
/// </summary>
public interface IEmailService
{
    Task<bool> SendEmailAsync(string to, string subject, string htmlBody);
}
