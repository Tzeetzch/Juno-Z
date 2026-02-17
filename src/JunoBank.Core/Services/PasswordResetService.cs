using System.Security.Cryptography;
using JunoBank.Web.Data;
using JunoBank.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace JunoBank.Web.Services;

/// <summary>
/// Service for managing password reset tokens with rate limiting and token invalidation.
/// </summary>
public class PasswordResetService : IPasswordResetService
{
    private readonly AppDbContext _context;
    private readonly TimeProvider _timeProvider;

    private const int TokenExpiryMinutes = 15;
    private const int MaxRequestsPerHour = 3;
    private const string DemoEmailDomain = "@junobank.local";

    public PasswordResetService(AppDbContext context, TimeProvider timeProvider)
    {
        _context = context;
        _timeProvider = timeProvider;
    }

    public async Task<string?> CreateResetTokenAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        var normalizedEmail = email.Trim().ToLowerInvariant();

        // Demo accounts cannot reset passwords
        if (IsDemoAccount(normalizedEmail))
            return null;

        // Find user by email
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == normalizedEmail);

        if (user == null)
            return null;

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var oneHourAgo = now.AddHours(-1);

        // Check rate limiting: max 3 requests per email per hour
        var recentTokenCount = await _context.PasswordResetTokens
            .CountAsync(t => t.UserId == user.Id && t.CreatedAt >= oneHourAgo);

        if (recentTokenCount >= MaxRequestsPerHour)
            return null;

        // Invalidate all existing unused tokens for this user
        var existingTokens = await _context.PasswordResetTokens
            .Where(t => t.UserId == user.Id && t.UsedAt == null)
            .ToListAsync();

        foreach (var existingToken in existingTokens)
        {
            existingToken.UsedAt = now; // Mark as used (invalidated)
        }

        // Generate new token
        var token = GenerateSecureToken();
        var resetToken = new PasswordResetToken
        {
            UserId = user.Id,
            Token = token,
            ExpiresAt = now.AddMinutes(TokenExpiryMinutes),
            CreatedAt = now
        };

        _context.PasswordResetTokens.Add(resetToken);
        await _context.SaveChangesAsync();

        return token;
    }

    public async Task<int?> ValidateTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var resetToken = await _context.PasswordResetTokens
            .FirstOrDefaultAsync(t => t.Token == token);

        if (resetToken == null)
            return null;

        // Check if already used
        if (resetToken.UsedAt != null)
            return null;

        // Check if expired
        if (resetToken.ExpiresAt < now)
            return null;

        return resetToken.UserId;
    }

    public async Task<bool> ResetPasswordAsync(string token, string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(newPasswordHash))
            return false;

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var resetToken = await _context.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == token);

        if (resetToken == null)
            return false;

        // Check if already used
        if (resetToken.UsedAt != null)
            return false;

        // Check if expired
        if (resetToken.ExpiresAt < now)
            return false;

        // Check if user's email is a demo account
        if (resetToken.User?.Email != null && IsDemoAccount(resetToken.User.Email))
            return false;

        // Update password
        if (resetToken.User != null)
        {
            resetToken.User.PasswordHash = newPasswordHash;
        }

        // Mark token as used
        resetToken.UsedAt = now;

        await _context.SaveChangesAsync();
        return true;
    }

    public bool IsDemoAccount(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        return email.Trim().ToLowerInvariant().EndsWith(DemoEmailDomain, StringComparison.OrdinalIgnoreCase);
    }

    private static string GenerateSecureToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }
}
