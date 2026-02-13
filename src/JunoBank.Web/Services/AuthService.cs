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
    private readonly IAuthStateProvider _authProvider;
    private readonly TimeProvider _timeProvider;

    public AuthService(
        AppDbContext db,
        IPasswordService passwordService,
        IAuthStateProvider authProvider,
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

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        // Check lockout (before password validation to prevent timing attacks)
        if (user.LockoutUntil.HasValue && user.LockoutUntil > now)
        {
            var remaining = (int)(user.LockoutUntil.Value - now).TotalMinutes + 1;
            return AuthResult.LockedOut(remaining, user.LockoutUntil.Value);
        }

        if (!_passwordService.VerifyPassword(password, user.PasswordHash ?? string.Empty))
        {
            // Failed attempt - increment counter
            user.FailedLoginAttempts = (user.FailedLoginAttempts ?? 0) + 1;

            if (user.FailedLoginAttempts >= 5)
            {
                user.LockoutUntil = now.AddMinutes(5);
                await _db.SaveChangesAsync();
                return AuthResult.LockedOut(5, user.LockoutUntil.Value);
            }

            await _db.SaveChangesAsync();
            var attemptsRemaining = 5 - user.FailedLoginAttempts.Value;
            return AuthResult.FailedWithAttemptsRemaining(attemptsRemaining);
        }

        // Success! Reset failed attempts
        user.FailedLoginAttempts = 0;
        user.LockoutUntil = null;
        await _db.SaveChangesAsync();

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
        // Legacy method - get the first child and authenticate
        var firstChild = await _db.Users
            .Where(u => u.Role == UserRole.Child)
            .OrderBy(u => u.Id)
            .FirstOrDefaultAsync();

        if (firstChild == null)
        {
            return AuthResult.Failed("No account found");
        }

        return await AuthenticateChildByIdAsync(firstChild.Id, pictureSequence);
    }

    /// <inheritdoc />
    public async Task<AuthResult> AuthenticateChildByIdAsync(int childId, string[] pictureSequence)
    {
        // Get the specific child user with their picture password
        var child = await _db.Users
            .Include(u => u.PicturePassword)
            .FirstOrDefaultAsync(u => u.Id == childId && u.Role == UserRole.Child);

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
            return AuthResult.LockedOut(remaining, picturePassword.LockedUntil.Value);
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
            return AuthResult.LockedOut(5, picturePassword.LockedUntil.Value);
        }

        await _db.SaveChangesAsync();
        var attemptsRemaining = 5 - picturePassword.FailedAttempts;
        return AuthResult.FailedWithAttemptsRemaining(attemptsRemaining);
    }

    /// <inheritdoc />
    public async Task<List<ChildLoginInfo>> GetChildrenForLoginAsync()
    {
        return await _db.Users
            .Where(u => u.Role == UserRole.Child)
            .OrderBy(u => u.Name)
            .Select(u => new ChildLoginInfo
            {
                Id = u.Id,
                Name = u.Name
            })
            .ToListAsync();
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
