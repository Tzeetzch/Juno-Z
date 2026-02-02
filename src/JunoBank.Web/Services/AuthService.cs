using JunoBank.Web.Auth;
using JunoBank.Web.Data;
using JunoBank.Web.Data.Entities;
using JunoBank.Web.Utils;
using Microsoft.EntityFrameworkCore;

namespace JunoBank.Web.Services;

/// <summary>
/// Implementation of IAuthService that handles parent and child authentication.
/// </summary>
public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IPasswordService _passwordService;
    private readonly CustomAuthStateProvider _authProvider;
    private readonly TimeProvider _timeProvider;

    public AuthService(
        AppDbContext db,
        IPasswordService passwordService,
        CustomAuthStateProvider authProvider,
        TimeProvider timeProvider)
    {
        _db = db;
        _passwordService = passwordService;
        _authProvider = authProvider;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async Task<AuthResult> AuthenticateParentAsync(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return AuthResult.Failed("Invalid email or password");
        }

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == email && u.Role == UserRole.Parent);

        if (user == null)
        {
            return AuthResult.Failed("Invalid email or password");
        }

        if (!_passwordService.VerifyPassword(password, user.PasswordHash ?? string.Empty))
        {
            return AuthResult.Failed("Invalid email or password");
        }

        var session = new UserSession
        {
            UserId = user.Id,
            UserName = user.Name,
            Role = user.Role
        };

        await _authProvider.LoginAsync(session);
        return AuthResult.Succeeded(session);
    }

    /// <inheritdoc />
    public async Task<AuthResult> AuthenticateChildAsync(string[] pictureSequence)
    {
        // Get the child user with their picture password
        var child = await _db.Users
            .Include(u => u.PicturePassword)
            .FirstOrDefaultAsync(u => u.Role == UserRole.Child);

        if (child?.PicturePassword == null)
        {
            return AuthResult.Failed("No account found");
        }

        var picturePassword = child.PicturePassword;
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        // Check lockout
        if (picturePassword.LockedUntil.HasValue && picturePassword.LockedUntil > now)
        {
            var remaining = (int)(picturePassword.LockedUntil.Value - now).TotalMinutes + 1;
            return AuthResult.LockedOut(remaining);
        }

        // Hash the sequence and compare
        var sequenceHash = SecurityUtils.HashPictureSequence(string.Join(",", pictureSequence));

        if (sequenceHash == picturePassword.ImageSequenceHash)
        {
            // Success! Reset failed attempts
            picturePassword.FailedAttempts = 0;
            picturePassword.LockedUntil = null;
            await _db.SaveChangesAsync();

            var session = new UserSession
            {
                UserId = child.Id,
                UserName = child.Name,
                Role = child.Role
            };

            await _authProvider.LoginAsync(session);
            return AuthResult.Succeeded(session);
        }

        // Failed attempt
        picturePassword.FailedAttempts++;

        if (picturePassword.FailedAttempts >= 5)
        {
            picturePassword.LockedUntil = now.AddMinutes(5);
            await _db.SaveChangesAsync();
            return AuthResult.LockedOut(5);
        }

        await _db.SaveChangesAsync();
        var attemptsRemaining = 5 - picturePassword.FailedAttempts;
        return AuthResult.FailedWithAttemptsRemaining(attemptsRemaining);
    }

    /// <inheritdoc />
    public async Task LogoutAsync()
    {
        await _authProvider.LogoutAsync();
    }

    /// <inheritdoc />
    public async Task<UserSession?> GetCurrentUserAsync()
    {
        return await _authProvider.GetCurrentUserAsync();
    }
}
