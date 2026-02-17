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
    /// Legacy method - authenticates the first child found.
    /// </summary>
    /// <returns>AuthResult with success/failure and any error message</returns>
    Task<AuthResult> AuthenticateChildAsync(string[] pictureSequence);

    /// <summary>
    /// Attempts to authenticate a specific child user with picture password sequence.
    /// </summary>
    /// <param name="childId">The ID of the child to authenticate</param>
    /// <param name="pictureSequence">The picture sequence entered by the child</param>
    /// <returns>AuthResult with success/failure and any error message</returns>
    Task<AuthResult> AuthenticateChildByIdAsync(int childId, string[] pictureSequence);

    /// <summary>
    /// Gets all children available for login.
    /// </summary>
    /// <returns>List of children with their Id and Name</returns>
    Task<List<ChildLoginInfo>> GetChildrenForLoginAsync();

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
    public DateTime? LockoutUntil { get; init; }
    public int? AttemptsRemaining { get; init; }

    public static AuthResult Succeeded(UserSession session) =>
        new() { Success = true, Session = session };

    public static AuthResult Failed(string error) =>
        new() { Success = false, Error = error };

    public static AuthResult LockedOut(int minutesRemaining, DateTime lockoutUntil) =>
        new() { Success = false, IsLockedOut = true, LockoutMinutesRemaining = minutesRemaining, LockoutUntil = lockoutUntil, Error = $"Too many attempts. Try again in {minutesRemaining} minutes." };

    public static AuthResult FailedWithAttemptsRemaining(int remaining) =>
        new() { Success = false, AttemptsRemaining = remaining, Error = $"Wrong sequence. {remaining} tries left." };
}

/// <summary>
/// Information about a child for the login picker.
/// </summary>
public class ChildLoginInfo
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
}
