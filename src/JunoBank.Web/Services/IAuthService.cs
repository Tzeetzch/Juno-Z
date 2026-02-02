using JunoBank.Web.Auth;
using JunoBank.Web.Data.Entities;

namespace JunoBank.Web.Services;

/// <summary>
/// Authentication service for handling login/logout logic.
/// Decouples auth from UI components for better testability.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Attempts to authenticate a parent user with email and password.
    /// </summary>
    /// <returns>UserSession if successful, null otherwise</returns>
    Task<AuthResult> AuthenticateParentAsync(string email, string password);

    /// <summary>
    /// Attempts to authenticate a child user with picture password sequence.
    /// </summary>
    /// <returns>AuthResult with success/failure and any error message</returns>
    Task<AuthResult> AuthenticateChildAsync(string[] pictureSequence);

    /// <summary>
    /// Logs out the current user.
    /// </summary>
    Task LogoutAsync();

    /// <summary>
    /// Gets the currently authenticated user, if any.
    /// </summary>
    Task<UserSession?> GetCurrentUserAsync();
}

/// <summary>
/// Result of an authentication attempt.
/// </summary>
public class AuthResult
{
    public bool Success { get; init; }
    public UserSession? Session { get; init; }
    public string? Error { get; init; }
    public bool IsLockedOut { get; init; }
    public int? LockoutMinutesRemaining { get; init; }
    public int? AttemptsRemaining { get; init; }

    public static AuthResult Succeeded(UserSession session) =>
        new() { Success = true, Session = session };

    public static AuthResult Failed(string error) =>
        new() { Success = false, Error = error };

    public static AuthResult LockedOut(int minutesRemaining) =>
        new() { Success = false, IsLockedOut = true, LockoutMinutesRemaining = minutesRemaining, Error = $"Too many attempts. Try again in {minutesRemaining} minutes." };

    public static AuthResult FailedWithAttemptsRemaining(int remaining) =>
        new() { Success = false, AttemptsRemaining = remaining, Error = $"Wrong sequence. {remaining} tries left." };
}
