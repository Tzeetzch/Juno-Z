using System.Security.Cryptography;
using System.Text;

namespace JunoBank.Web.Utils;

/// <summary>
/// Centralized security utilities for hashing operations.
/// </summary>
public static class SecurityUtils
{
    /// <summary>
    /// Hashes a picture password sequence using SHA256.
    /// Used for child login picture sequences.
    /// </summary>
    /// <param name="sequence">Comma-separated image identifiers (e.g., "cat,dog,star,moon")</param>
    /// <returns>Base64-encoded hash of the sequence</returns>
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
