using JunoBank.Tests.Helpers;
using JunoBank.Web.Auth;
using JunoBank.Web.Data.Entities;
using JunoBank.Web.Services;
using JunoBank.Web.Utils;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace JunoBank.Tests.Services;

public class AuthServiceTests : DatabaseTestBase
{
    private readonly FakeTimeProvider _timeProvider;
    private readonly Mock<IPasswordService> _passwordServiceMock;
    private readonly Mock<IAuthStateProvider> _authProviderMock;
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 1, 15, 10, 0, 0, TimeSpan.Zero));
        _passwordServiceMock = new Mock<IPasswordService>();
        _authProviderMock = new Mock<IAuthStateProvider>();
        _authProviderMock.Setup(x => x.LoginAsync(It.IsAny<UserSession>())).Returns(Task.CompletedTask);
        
        _service = new AuthService(Db, _passwordServiceMock.Object, _authProviderMock.Object, _timeProvider);
    }

    #region GetChildrenForLoginAsync Tests

    [Fact]
    public async Task GetChildrenForLoginAsync_ReturnsAllChildren()
    {
        // Arrange
        var child1 = new User { Name = "Junior", Role = UserRole.Child, Balance = 10.00m };
        var child2 = new User { Name = "Sophie", Role = UserRole.Child, Balance = 5.00m };
        var parent = new User { Name = "Dad", Role = UserRole.Parent, Email = "dad@test.com", PasswordHash = "hash" };
        
        Db.Users.AddRange(child1, child2, parent);
        await Db.SaveChangesAsync();

        // Act
        var result = await _service.GetChildrenForLoginAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, c => c.Name == "Junior");
        Assert.Contains(result, c => c.Name == "Sophie");
    }

    [Fact]
    public async Task GetChildrenForLoginAsync_ReturnsEmptyWhenNoChildren()
    {
        // Arrange
        var parent = new User { Name = "Dad", Role = UserRole.Parent, Email = "dad@test.com", PasswordHash = "hash" };
        Db.Users.Add(parent);
        await Db.SaveChangesAsync();

        // Act
        var result = await _service.GetChildrenForLoginAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetChildrenForLoginAsync_SortsAlphabetically()
    {
        // Arrange
        var child1 = new User { Name = "Zoe", Role = UserRole.Child, Balance = 10.00m };
        var child2 = new User { Name = "Adam", Role = UserRole.Child, Balance = 5.00m };
        var child3 = new User { Name = "Beth", Role = UserRole.Child, Balance = 3.00m };
        
        Db.Users.AddRange(child1, child2, child3);
        await Db.SaveChangesAsync();

        // Act
        var result = await _service.GetChildrenForLoginAsync();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("Adam", result[0].Name);
        Assert.Equal("Beth", result[1].Name);
        Assert.Equal("Zoe", result[2].Name);
    }

    #endregion

    #region AuthenticateChildByIdAsync Tests

    [Fact]
    public async Task AuthenticateChildByIdAsync_ReturnsSuccess_WhenCorrectSequence()
    {
        // Arrange
        var child = new User { Name = "Junior", Role = UserRole.Child, Balance = 10.00m };
        Db.Users.Add(child);
        await Db.SaveChangesAsync();

        var picturePassword = new PicturePassword
        {
            UserId = child.Id,
            ImageSequenceHash = SecurityUtils.HashPictureSequence("cat,dog,star,moon"),
            GridSize = 9,
            SequenceLength = 4
        };
        Db.PicturePasswords.Add(picturePassword);
        await Db.SaveChangesAsync();

        // Act
        var result = await _service.AuthenticateChildByIdAsync(child.Id, ["cat", "dog", "star", "moon"]);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Session);
        Assert.Equal("Junior", result.Session.UserName);
        Assert.Equal(UserRole.Child, result.Session.Role);
    }

    [Fact]
    public async Task AuthenticateChildByIdAsync_ReturnsFailed_WhenWrongSequence()
    {
        // Arrange
        var child = new User { Name = "Junior", Role = UserRole.Child, Balance = 10.00m };
        Db.Users.Add(child);
        await Db.SaveChangesAsync();

        var picturePassword = new PicturePassword
        {
            UserId = child.Id,
            ImageSequenceHash = SecurityUtils.HashPictureSequence("cat,dog,star,moon"),
            GridSize = 9,
            SequenceLength = 4
        };
        Db.PicturePasswords.Add(picturePassword);
        await Db.SaveChangesAsync();

        // Act
        var result = await _service.AuthenticateChildByIdAsync(child.Id, ["cat", "dog", "moon", "star"]);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.AttemptsRemaining);
        Assert.Equal(4, result.AttemptsRemaining);
    }

    [Fact]
    public async Task AuthenticateChildByIdAsync_ReturnsFailed_WhenChildNotFound()
    {
        // Act
        var result = await _service.AuthenticateChildByIdAsync(999, ["cat", "dog", "star", "moon"]);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("No account found", result.Error);
    }

    [Fact]
    public async Task AuthenticateChildByIdAsync_AuthenticatesCorrectChild()
    {
        // Arrange - two children with different passwords
        var child1 = new User { Name = "Junior", Role = UserRole.Child, Balance = 10.00m };
        var child2 = new User { Name = "Sophie", Role = UserRole.Child, Balance = 5.00m };
        Db.Users.AddRange(child1, child2);
        await Db.SaveChangesAsync();

        var picturePassword1 = new PicturePassword
        {
            UserId = child1.Id,
            ImageSequenceHash = SecurityUtils.HashPictureSequence("cat,dog,star,moon"),
            GridSize = 9,
            SequenceLength = 4
        };
        var picturePassword2 = new PicturePassword
        {
            UserId = child2.Id,
            ImageSequenceHash = SecurityUtils.HashPictureSequence("star,moon,cat,dog"),
            GridSize = 9,
            SequenceLength = 4
        };
        Db.PicturePasswords.AddRange(picturePassword1, picturePassword2);
        await Db.SaveChangesAsync();

        // Act - authenticate Sophie with her password
        var result = await _service.AuthenticateChildByIdAsync(child2.Id, ["star", "moon", "cat", "dog"]);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Session);
        Assert.Equal("Sophie", result.Session.UserName);
        Assert.Equal(child2.Id, result.Session.UserId);
    }

    [Fact]
    public async Task AuthenticateChildByIdAsync_FailsWithWrongChildsPassword()
    {
        // Arrange - two children with different passwords
        var child1 = new User { Name = "Junior", Role = UserRole.Child, Balance = 10.00m };
        var child2 = new User { Name = "Sophie", Role = UserRole.Child, Balance = 5.00m };
        Db.Users.AddRange(child1, child2);
        await Db.SaveChangesAsync();

        var picturePassword1 = new PicturePassword
        {
            UserId = child1.Id,
            ImageSequenceHash = SecurityUtils.HashPictureSequence("cat,dog,star,moon"),
            GridSize = 9,
            SequenceLength = 4
        };
        var picturePassword2 = new PicturePassword
        {
            UserId = child2.Id,
            ImageSequenceHash = SecurityUtils.HashPictureSequence("star,moon,cat,dog"),
            GridSize = 9,
            SequenceLength = 4
        };
        Db.PicturePasswords.AddRange(picturePassword1, picturePassword2);
        await Db.SaveChangesAsync();

        // Act - try to authenticate Junior with Sophie's password
        var result = await _service.AuthenticateChildByIdAsync(child1.Id, ["star", "moon", "cat", "dog"]);

        // Assert
        Assert.False(result.Success);
    }

    #endregion
}
