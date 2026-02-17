namespace JunoBank.Web.Services;

/// <summary>
/// Centralized password hashing and verification service.
/// Abstracts BCrypt implementation for easier testing and potential algorithm changes.
/// </summary>
public interface IPasswordService
{
    /// <summary>
    /// Hashes a plaintext password using BCrypt.
    /// </summary>
    /// <param name="password">The plaintext password to hash</param>
    /// <returns>BCrypt hash of the password</returns>
    string HashPassword(string password);

    /// <summary>
    /// Verifies a plaintext password against a stored hash.
    /// </summary>
    /// <param name="password">The plaintext password to verify</param>
    /// <param name="hash">The stored BCrypt hash</param>
    /// <returns>True if the password matches the hash</returns>
    bool VerifyPassword(string password, string hash);
}

/// <summary>
/// BCrypt implementation of IPasswordService.
/// Uses BCrypt.Net-Next with default work factor.
/// </summary>
public class PasswordService : IPasswordService
{
    /// <inheritdoc />
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    /// <inheritdoc />
    public bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
