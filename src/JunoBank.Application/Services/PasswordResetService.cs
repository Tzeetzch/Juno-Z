using System.Security.Cryptography;
using JunoBank.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JunoBank.Application.Services;

/// <summary>
/// Service for managing password reset tokens with rate limiting and token invalidation.
/// </summary>
public class PasswordResetService : IPasswordResetService
{
    private readonly IAppDbContext _context;
    private readonly TimeProvider _timeProvider;

    private const int TokenExpiryMinutes = 15;
    private const int MaxRequestsPerHour = 3;
    private const string DemoEmailDomain = "@junobank.local";

    public PasswordResetService(IAppDbContext context, TimeProvider timeProvider)
    {
        _context = context;
        _timeProvider = timeProvider;
    }

    public async Task<string?> CreateResetTokenAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        var normalizedEmail = email.Trim().ToLowerInvariant();

        if (IsDemoAccount(normalizedEmail))
            return null;

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == normalizedEmail);

        if (user == null)
            return null;

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var oneHourAgo = now.AddHours(-1);

        var recentTokenCount = await _context.PasswordResetTokens
            .CountAsync(t => t.UserId == user.Id && t.CreatedAt >= oneHourAgo);

        if (recentTokenCount >= MaxRequestsPerHour)
            return null;

        var existingTokens = await _context.PasswordResetTokens
            .Where(t => t.UserId == user.Id && t.UsedAt == null)
            .ToListAsync();

        foreach (var existingToken in existingTokens)
        {
            existingToken.UsedAt = now;
        }

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

        if (resetToken.UsedAt != null)
            return null;

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

        if (resetToken.UsedAt != null)
            return false;

        if (resetToken.ExpiresAt < now)
            return false;

        if (resetToken.User?.Email != null && IsDemoAccount(resetToken.User.Email))
            return false;

        if (resetToken.User != null)
        {
            resetToken.User.PasswordHash = newPasswordHash;
        }

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
