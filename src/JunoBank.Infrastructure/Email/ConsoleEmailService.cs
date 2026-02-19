namespace JunoBank.Infrastructure.Email;

/// <summary>
/// Console email service for development and testing.
/// </summary>
public class ConsoleEmailService : IEmailService
{
    private readonly ILogger<ConsoleEmailService> _logger;

    public ConsoleEmailService(ILogger<ConsoleEmailService> logger)
    {
        _logger = logger;
    }

    public Task<bool> SendEmailAsync(string to, string subject, string htmlBody)
    {
        _logger.LogInformation(
            "EMAIL (Console Mode)\n" +
            "   To: {To}\n" +
            "   Subject: {Subject}\n" +
            "   Body:\n{Body}",
            to, subject, htmlBody);

        return Task.FromResult(true);
    }
}
