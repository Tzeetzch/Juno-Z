using JunoBank.Application.DTOs;

namespace JunoBank.Application.Interfaces;

/// <summary>
/// Interface for auth state provider operations.
/// </summary>
public interface IAuthStateProvider
{
    Task LoginAsync(UserSession session);
    Task LogoutAsync();
    Task<UserSession?> GetCurrentUserAsync();
}
