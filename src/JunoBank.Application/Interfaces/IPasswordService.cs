namespace JunoBank.Application.Interfaces;

/// <summary>
/// Centralized password hashing and verification service.
/// </summary>
public interface IPasswordService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}
