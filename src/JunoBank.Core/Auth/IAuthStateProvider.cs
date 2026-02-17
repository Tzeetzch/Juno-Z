namespace JunoBank.Web.Auth;

/// <summary>
/// Interface for auth state provider operations.
/// Enables mocking in tests without depending on concrete implementation.
/// </summary>
public interface IAuthStateProvider
{
    Task LoginAsync(UserSession session);
    Task LogoutAsync();
    Task<UserSession?> GetCurrentUserAsync();
}
