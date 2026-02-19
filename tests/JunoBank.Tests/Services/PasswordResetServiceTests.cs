using JunoBank.Tests.Helpers;
using JunoBank.Domain.Entities;
using JunoBank.Domain.Enums;
using JunoBank.Application.Interfaces;
using JunoBank.Application.Services;
using Microsoft.Extensions.Time.Testing;

namespace JunoBank.Tests.Services;

public class PasswordResetServiceTests : DatabaseTestBase
{
    private readonly FakeTimeProvider _timeProvider;
    private readonly PasswordResetService _service;

    public PasswordResetServiceTests()
    {
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 1, 15, 10, 0, 0, TimeSpan.Zero));
        _service = new PasswordResetService(Db, _timeProvider);
    }

    private async Task<User> CreateTestUserAsync(string email = "test@example.com")
    {
        var user = new User
        {
            Name = "Test User",
            Email = email,
            Role = UserRole.Parent,
            PasswordHash = "oldhash123"
        };
        Db.Users.Add(user);
        await Db.SaveChangesAsync();
        return user;
    }

    #region IsDemoAccount Tests

    [Theory]
    [InlineData("parent@junobank.local", true)]
    [InlineData("CHILD@JUNOBANK.LOCAL", true)]
    [InlineData("test@junobank.local", true)]
    [InlineData("parent@example.com", false)]
    [InlineData("user@gmail.com", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsDemoAccount_ReturnsCorrectResult(string? email, bool expected)
    {
        var result = _service.IsDemoAccount(email ?? "");
        Assert.Equal(expected, result);
    }

    #endregion

    #region CreateResetTokenAsync Tests

    [Fact]
    public async Task CreateResetTokenAsync_ValidEmail_ReturnsToken()
    {
        var user = await CreateTestUserAsync("parent@example.com");

        var token = await _service.CreateResetTokenAsync("parent@example.com");

        Assert.NotNull(token);
        Assert.NotEmpty(token);
    }

    [Fact]
    public async Task CreateResetTokenAsync_NonExistentEmail_ReturnsNull()
    {
        var token = await _service.CreateResetTokenAsync("nonexistent@example.com");

        Assert.Null(token);
    }

    [Fact]
    public async Task CreateResetTokenAsync_DemoAccount_ReturnsNull()
    {
        var user = await CreateTestUserAsync("parent@junobank.local");

        var token = await _service.CreateResetTokenAsync("parent@junobank.local");

        Assert.Null(token);
    }

    [Fact]
    public async Task CreateResetTokenAsync_EmptyEmail_ReturnsNull()
    {
        var token = await _service.CreateResetTokenAsync("");

        Assert.Null(token);
    }

    [Fact]
    public async Task CreateResetTokenAsync_InvalidatesOldTokens()
    {
        var user = await CreateTestUserAsync("parent@example.com");

        // Create first token
        var token1 = await _service.CreateResetTokenAsync("parent@example.com");
        Assert.NotNull(token1);

        // Create second token
        var token2 = await _service.CreateResetTokenAsync("parent@example.com");
        Assert.NotNull(token2);

        // First token should be invalid now
        var validatedUser = await _service.ValidateTokenAsync(token1);
        Assert.Null(validatedUser);

        // Second token should be valid
        var validatedUser2 = await _service.ValidateTokenAsync(token2);
        Assert.NotNull(validatedUser2);
    }

    [Fact]
    public async Task CreateResetTokenAsync_RateLimiting_BlocksAfterThreeRequests()
    {
        var user = await CreateTestUserAsync("parent@example.com");

        // Make 3 requests (should succeed)
        var token1 = await _service.CreateResetTokenAsync("parent@example.com");
        var token2 = await _service.CreateResetTokenAsync("parent@example.com");
        var token3 = await _service.CreateResetTokenAsync("parent@example.com");

        Assert.NotNull(token1);
        Assert.NotNull(token2);
        Assert.NotNull(token3);

        // 4th request should be blocked
        var token4 = await _service.CreateResetTokenAsync("parent@example.com");
        Assert.Null(token4);
    }

    [Fact]
    public async Task CreateResetTokenAsync_RateLimiting_ResetsAfterOneHour()
    {
        var user = await CreateTestUserAsync("parent@example.com");

        // Make 3 requests
        await _service.CreateResetTokenAsync("parent@example.com");
        await _service.CreateResetTokenAsync("parent@example.com");
        await _service.CreateResetTokenAsync("parent@example.com");

        // Advance time by 1 hour + 1 minute
        _timeProvider.Advance(TimeSpan.FromMinutes(61));

        // Should be able to request again
        var token = await _service.CreateResetTokenAsync("parent@example.com");
        Assert.NotNull(token);
    }

    [Fact]
    public async Task CreateResetTokenAsync_CaseInsensitiveEmail()
    {
        var user = await CreateTestUserAsync("Parent@Example.COM");

        var token = await _service.CreateResetTokenAsync("parent@example.com");

        Assert.NotNull(token);
    }

    #endregion

    #region ValidateTokenAsync Tests

    [Fact]
    public async Task ValidateTokenAsync_ValidToken_ReturnsUserId()
    {
        var user = await CreateTestUserAsync("parent@example.com");
        var token = await _service.CreateResetTokenAsync("parent@example.com");

        var userId = await _service.ValidateTokenAsync(token!);

        Assert.Equal(user.Id, userId);
    }

    [Fact]
    public async Task ValidateTokenAsync_InvalidToken_ReturnsNull()
    {
        var userId = await _service.ValidateTokenAsync("invalid-token");

        Assert.Null(userId);
    }

    [Fact]
    public async Task ValidateTokenAsync_ExpiredToken_ReturnsNull()
    {
        var user = await CreateTestUserAsync("parent@example.com");
        var token = await _service.CreateResetTokenAsync("parent@example.com");

        // Advance time by 16 minutes (token expires at 15)
        _timeProvider.Advance(TimeSpan.FromMinutes(16));

        var userId = await _service.ValidateTokenAsync(token!);

        Assert.Null(userId);
    }

    [Fact]
    public async Task ValidateTokenAsync_UsedToken_ReturnsNull()
    {
        var user = await CreateTestUserAsync("parent@example.com");
        var token = await _service.CreateResetTokenAsync("parent@example.com");

        // Use the token
        await _service.ResetPasswordAsync(token!, "newhash123");

        // Should be invalid now
        var userId = await _service.ValidateTokenAsync(token!);

        Assert.Null(userId);
    }

    [Fact]
    public async Task ValidateTokenAsync_EmptyToken_ReturnsNull()
    {
        var userId = await _service.ValidateTokenAsync("");

        Assert.Null(userId);
    }

    #endregion

    #region ResetPasswordAsync Tests

    [Fact]
    public async Task ResetPasswordAsync_ValidToken_UpdatesPassword()
    {
        var user = await CreateTestUserAsync("parent@example.com");
        var token = await _service.CreateResetTokenAsync("parent@example.com");

        var result = await _service.ResetPasswordAsync(token!, "newhash456");

        Assert.True(result);

        // Verify password was updated
        await Db.Entry(user).ReloadAsync();
        Assert.Equal("newhash456", user.PasswordHash);
    }

    [Fact]
    public async Task ResetPasswordAsync_InvalidToken_ReturnsFalse()
    {
        var result = await _service.ResetPasswordAsync("invalid-token", "newhash456");

        Assert.False(result);
    }

    [Fact]
    public async Task ResetPasswordAsync_ExpiredToken_ReturnsFalse()
    {
        var user = await CreateTestUserAsync("parent@example.com");
        var token = await _service.CreateResetTokenAsync("parent@example.com");

        // Advance time by 16 minutes
        _timeProvider.Advance(TimeSpan.FromMinutes(16));

        var result = await _service.ResetPasswordAsync(token!, "newhash456");

        Assert.False(result);

        // Password should not have changed
        await Db.Entry(user).ReloadAsync();
        Assert.Equal("oldhash123", user.PasswordHash);
    }

    [Fact]
    public async Task ResetPasswordAsync_UsedToken_ReturnsFalse()
    {
        var user = await CreateTestUserAsync("parent@example.com");
        var token = await _service.CreateResetTokenAsync("parent@example.com");

        // First reset should succeed
        var result1 = await _service.ResetPasswordAsync(token!, "newhash456");
        Assert.True(result1);

        // Second reset with same token should fail
        var result2 = await _service.ResetPasswordAsync(token!, "anotherhash");
        Assert.False(result2);

        // Password should remain from first reset
        await Db.Entry(user).ReloadAsync();
        Assert.Equal("newhash456", user.PasswordHash);
    }

    [Fact]
    public async Task ResetPasswordAsync_EmptyPassword_ReturnsFalse()
    {
        var user = await CreateTestUserAsync("parent@example.com");
        var token = await _service.CreateResetTokenAsync("parent@example.com");

        var result = await _service.ResetPasswordAsync(token!, "");

        Assert.False(result);
    }

    [Fact]
    public async Task ResetPasswordAsync_MarksTokenAsUsed()
    {
        var user = await CreateTestUserAsync("parent@example.com");
        var token = await _service.CreateResetTokenAsync("parent@example.com");

        await _service.ResetPasswordAsync(token!, "newhash456");

        var tokenEntity = Db.PasswordResetTokens.First(t => t.Token == token);
        Assert.NotNull(tokenEntity.UsedAt);
    }

    #endregion
}
