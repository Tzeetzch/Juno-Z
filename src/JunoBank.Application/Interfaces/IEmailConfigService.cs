using JunoBank.Application.DTOs;

namespace JunoBank.Application.Interfaces;

/// <summary>
/// Service for reading, writing, and testing email (SMTP) configuration.
/// </summary>
public interface IEmailConfigService
{
    bool IsConfigured { get; }
    EmailConfigData? GetEmailConfig();
    Task SaveEmailConfigAsync(EmailConfigData config);
    Task<bool> SendTestEmailAsync(EmailConfigData config, string targetEmail);
}
