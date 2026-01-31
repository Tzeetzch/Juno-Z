namespace JunoBank.Web.Data.Entities;

/// <summary>
/// Password reset token for parent accounts.
/// Tokens expire after 15 minutes and can only be used once.
/// </summary>
public class PasswordResetToken
{
    public int Id { get; set; }
    
    /// <summary>
    /// The user this token is for.
    /// </summary>
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    
    /// <summary>
    /// The unique token string (URL-safe random string).
    /// </summary>
    public string Token { get; set; } = string.Empty;
    
    /// <summary>
    /// When this token expires (15 minutes from creation).
    /// </summary>
    public DateTime ExpiresAt { get; set; }
    
    /// <summary>
    /// When this token was used. Null if not yet used.
    /// </summary>
    public DateTime? UsedAt { get; set; }
    
    /// <summary>
    /// When this token was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
