namespace JunoBank.Application.DTOs;

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
