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
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(sequence));
        return Convert.ToBase64String(bytes);
    }
}
