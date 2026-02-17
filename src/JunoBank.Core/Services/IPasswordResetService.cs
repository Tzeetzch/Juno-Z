namespace JunoBank.Web.Services;

/// <summary>
/// Service for managing password reset tokens and operations.
/// </summary>
public interface IPasswordResetService
{
    /// <summary>
    /// Creates a password reset token for the given email address.
    /// Returns null if email is not found, but does not reveal this to caller.
    /// </summary>
    /// <param name="email">The email address of the user requesting reset.</param>
    /// <returns>The reset token if successful, null if email not found or rate limited.</returns>
    Task<string?> CreateResetTokenAsync(string email);

    /// <summary>
    /// Validates a password reset token.
    /// </summary>
    /// <param name="token">The token to validate.</param>
    /// <returns>The user ID if valid, null if invalid or expired.</returns>
    Task<int?> ValidateTokenAsync(string token);

    /// <summary>
    /// Resets the password using a valid token.
    /// Marks the token as used after successful reset.
    /// </summary>
    /// <param name="token">The password reset token.</param>
    /// <param name="newPasswordHash">The new password hash.</param>
    /// <returns>True if password was reset successfully.</returns>
    Task<bool> ResetPasswordAsync(string token, string newPasswordHash);

    /// <summary>
    /// Checks if the email belongs to a demo account.
    /// Demo accounts cannot reset passwords.
    /// </summary>
    /// <param name="email">The email address to check.</param>
    /// <returns>True if demo account.</returns>
    bool IsDemoAccount(string email);
}
