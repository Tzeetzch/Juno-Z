namespace JunoBank.Application.Interfaces;

/// <summary>
/// Service for managing password reset tokens.
/// </summary>
public interface IPasswordResetService
{
    Task<string?> CreateResetTokenAsync(string email);
    Task<int?> ValidateTokenAsync(string token);
    Task<bool> ResetPasswordAsync(string token, string newPasswordHash);
    bool IsDemoAccount(string email);
}
