using JunoBank.Application.DTOs;

namespace JunoBank.Application.Interfaces;

/// <summary>
/// Authentication service for handling login/logout logic.
/// </summary>
public interface IAuthService
{
    Task<AuthResult> AuthenticateParentAsync(string email, string password);
    Task<AuthResult> AuthenticateChildAsync(string[] pictureSequence);
    Task<AuthResult> AuthenticateChildByIdAsync(int childId, string[] pictureSequence);
    Task<List<ChildLoginInfo>> GetChildrenForLoginAsync();
    Task LogoutAsync();
    Task<UserSession?> GetCurrentUserAsync();
}
