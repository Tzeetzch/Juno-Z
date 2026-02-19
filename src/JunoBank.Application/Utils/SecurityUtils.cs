using System.Security.Cryptography;
using System.Text;

namespace JunoBank.Application.Utils;

/// <summary>
/// Centralized security utilities for hashing operations.
/// </summary>
public static class SecurityUtils
{
    /// <summary>
    /// Hashes a picture password sequence using SHA256.
    /// </summary>
    public static string HashPictureSequence(string sequence)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(sequence));
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// Constant-time comparison of two hash strings to prevent timing attacks.
    /// </summary>
    public static bool ConstantTimeEquals(string a, string b)
    {
        var bytesA = Encoding.UTF8.GetBytes(a);
        var bytesB = Encoding.UTF8.GetBytes(b);
        return CryptographicOperations.FixedTimeEquals(bytesA, bytesB);
    }
}
