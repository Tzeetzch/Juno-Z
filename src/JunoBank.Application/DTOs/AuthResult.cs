namespace JunoBank.Application.DTOs;

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
