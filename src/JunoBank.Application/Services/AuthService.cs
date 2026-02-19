using JunoBank.Application.DTOs;
using JunoBank.Application.Interfaces;
using JunoBank.Application.Utils;
using Microsoft.EntityFrameworkCore;

namespace JunoBank.Application.Services;

/// <summary>
/// Implementation of IAuthService that handles parent and child authentication.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IAppDbContext _db;
    private readonly IPasswordService _passwordService;
    private readonly IAuthStateProvider _authProvider;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IAppDbContext db,
        IPasswordService passwordService,
        IAuthStateProvider authProvider,
        TimeProvider timeProvider,
        ILogger<AuthService> logger)
    {
        _db = db;
        _passwordService = passwordService;
        _authProvider = authProvider;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<AuthResult> AuthenticateParentAsync(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning("Parent login attempt with empty credentials");
            return AuthResult.Failed("Invalid email or password");
        }

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == email && u.Role == UserRole.Parent);

        if (user == null)
        {
            _passwordService.VerifyPassword(password, "$2a$11$aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
            _logger.LogWarning("Parent login attempt for unknown email");
            return AuthResult.Failed("Invalid email or password");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (user.LockoutUntil.HasValue && user.LockoutUntil > now)
        {
            _logger.LogWarning("Parent login blocked — account locked out (user {UserId})", user.Id);
            var remaining = (int)(user.LockoutUntil.Value - now).TotalMinutes + 1;
            return AuthResult.LockedOut(remaining, user.LockoutUntil.Value);
        }

        if (!_passwordService.VerifyPassword(password, user.PasswordHash ?? string.Empty))
        {
            user.FailedLoginAttempts = (user.FailedLoginAttempts ?? 0) + 1;
            _logger.LogWarning("Parent login failed for user {UserId} (attempt {Attempt}/5)", user.Id, user.FailedLoginAttempts);

            if (user.FailedLoginAttempts >= 5)
            {
                user.LockoutUntil = now.AddMinutes(5);
                await _db.SaveChangesAsync();
                _logger.LogWarning("Parent account {UserId} locked out for 5 minutes", user.Id);
                return AuthResult.LockedOut(5, user.LockoutUntil.Value);
            }

            await _db.SaveChangesAsync();
            var attemptsRemaining = 5 - user.FailedLoginAttempts.Value;
            return AuthResult.FailedWithAttemptsRemaining(attemptsRemaining);
        }

        user.FailedLoginAttempts = 0;
        user.LockoutUntil = null;
        await _db.SaveChangesAsync();
        _logger.LogInformation("Parent login successful for user {UserId}", user.Id);

        var session = new UserSession
        {
            UserId = user.Id,
            UserName = user.Name,
            Role = user.Role
        };

        await _authProvider.LoginAsync(session);
        return AuthResult.Succeeded(session);
    }

    public async Task<AuthResult> AuthenticateChildAsync(string[] pictureSequence)
    {
        var firstChild = await _db.Users
            .Where(u => u.Role == UserRole.Child)
            .OrderBy(u => u.Id)
            .FirstOrDefaultAsync();

        if (firstChild == null)
            return AuthResult.Failed("No account found");

        return await AuthenticateChildByIdAsync(firstChild.Id, pictureSequence);
    }

    public async Task<AuthResult> AuthenticateChildByIdAsync(int childId, string[] pictureSequence)
    {
        var child = await _db.Users
            .Include(u => u.PicturePassword)
            .FirstOrDefaultAsync(u => u.Id == childId && u.Role == UserRole.Child);

        if (child?.PicturePassword == null)
            return AuthResult.Failed("No account found");

        var picturePassword = child.PicturePassword;
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (picturePassword.LockedUntil.HasValue && picturePassword.LockedUntil > now)
        {
            _logger.LogWarning("Child login blocked — account locked out (child {ChildId})", childId);
            var remaining = (int)(picturePassword.LockedUntil.Value - now).TotalMinutes + 1;
            return AuthResult.LockedOut(remaining, picturePassword.LockedUntil.Value);
        }

        var sequenceHash = SecurityUtils.HashPictureSequence(string.Join(",", pictureSequence));

        if (SecurityUtils.ConstantTimeEquals(sequenceHash, picturePassword.ImageSequenceHash))
        {
            picturePassword.FailedAttempts = 0;
            picturePassword.LockedUntil = null;
            await _db.SaveChangesAsync();
            _logger.LogInformation("Child login successful for child {ChildId}", childId);

            var session = new UserSession
            {
                UserId = child.Id,
                UserName = child.Name,
                Role = child.Role
            };

            await _authProvider.LoginAsync(session);
            return AuthResult.Succeeded(session);
        }

        picturePassword.FailedAttempts++;
        _logger.LogWarning("Child login failed for child {ChildId} (attempt {Attempt}/5)", childId, picturePassword.FailedAttempts);

        if (picturePassword.FailedAttempts >= 5)
        {
            picturePassword.LockedUntil = now.AddMinutes(5);
            await _db.SaveChangesAsync();
            _logger.LogWarning("Child account {ChildId} locked out for 5 minutes", childId);
            return AuthResult.LockedOut(5, picturePassword.LockedUntil.Value);
        }

        await _db.SaveChangesAsync();
        var attemptsRemaining = 5 - picturePassword.FailedAttempts;
        return AuthResult.FailedWithAttemptsRemaining(attemptsRemaining);
    }

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

    public async Task LogoutAsync()
    {
        await _authProvider.LogoutAsync();
    }

    public async Task<UserSession?> GetCurrentUserAsync()
    {
        return await _authProvider.GetCurrentUserAsync();
    }
}
