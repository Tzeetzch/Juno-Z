using JunoBank.Tests.Helpers;
using JunoBank.Web.Data.Entities;
using JunoBank.Web.Services;
using JunoBank.Web.Utils;
using Moq;

namespace JunoBank.Tests.Services;

public class SetupServiceTests : DatabaseTestBase
{
    private readonly SetupService _service;

    public SetupServiceTests()
    {
        var emailConfig = new Mock<IEmailConfigService>();
        _service = new SetupService(Db, emailConfig.Object);
    }

    #region IsSetupRequiredAsync Tests

    [Fact]
    public async Task IsSetupRequiredAsync_ReturnsTrue_WhenNoAdminExists()
    {
        // Act
        var result = await _service.IsSetupRequiredAsync();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsSetupRequiredAsync_ReturnsFalse_WhenAdminExists()
    {
        // Arrange
        var admin = new User
        {
            Name = "Admin",
            Email = "admin@test.com",
            PasswordHash = "hash",
            Role = UserRole.Parent,
            IsAdmin = true
        };
        Db.Users.Add(admin);
        await Db.SaveChangesAsync();

        // Act
        var result = await _service.IsSetupRequiredAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsSetupRequiredAsync_ReturnsTrue_WhenParentExistsButNoAdmin()
    {
        // Arrange
        var parent = new User
        {
            Name = "Parent",
            Email = "parent@test.com",
            PasswordHash = "hash",
            Role = UserRole.Parent,
            IsAdmin = false
        };
        Db.Users.Add(parent);
        await Db.SaveChangesAsync();

        // Act
        var result = await _service.IsSetupRequiredAsync();

        // Assert
        Assert.True(result);
    }

    #endregion

    #region HasAdminAsync Tests

    [Fact]
    public async Task HasAdminAsync_ReturnsFalse_WhenNoUsers()
    {
        // Act
        var result = await _service.HasAdminAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task HasAdminAsync_ReturnsTrue_WhenAdminExists()
    {
        // Arrange
        var admin = new User
        {
            Name = "Admin",
            Email = "admin@test.com",
            PasswordHash = "hash",
            Role = UserRole.Parent,
            IsAdmin = true
        };
        Db.Users.Add(admin);
        await Db.SaveChangesAsync();

        // Act
        var result = await _service.HasAdminAsync();

        // Assert
        Assert.True(result);
    }

    #endregion

    #region CompleteSetupAsync Tests

    [Fact]
    public async Task CompleteSetupAsync_CreatesAdminUser()
    {
        // Arrange
        var setupData = new SetupData
        {
            Admin = new AdminData
            {
                Name = "Admin User",
                Email = "admin@test.com",
                Password = "Password123!"
            }
        };

        // Act
        var result = await _service.CompleteSetupAsync(setupData);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.AdminUserId);
        
        // Verify admin was created correctly
        var admin = await Db.Users.FindAsync(result.AdminUserId);
        Assert.NotNull(admin);
        Assert.Equal("Admin User", admin.Name);
        Assert.Equal("admin@test.com", admin.Email);
        Assert.True(admin.IsAdmin);
        Assert.Equal(UserRole.Parent, admin.Role);
        
        // Verify password is hashed
        Assert.True(BCrypt.Net.BCrypt.Verify("Password123!", admin.PasswordHash));
    }

    [Fact]
    public async Task CompleteSetupAsync_CreatesPartnerIfProvided()
    {
        // Arrange
        var setupData = new SetupData
        {
            Admin = new AdminData
            {
                Name = "Admin",
                Email = "admin@test.com",
                Password = "Pass1!"
            },
            Partner = new PartnerData
            {
                Name = "Partner",
                Email = "partner@test.com",
                Password = "Pass2!"
            }
        };

        // Act
        var result = await _service.CompleteSetupAsync(setupData);

        // Assert
        Assert.True(result.Success);
        
        // Verify partner was created
        var parents = Db.Users.Where(u => u.Role == UserRole.Parent).ToList();
        Assert.Equal(2, parents.Count);
        
        var partner = parents.FirstOrDefault(p => p.Name == "Partner");
        Assert.NotNull(partner);
        Assert.Equal("partner@test.com", partner.Email);
        Assert.False(partner.IsAdmin);
    }

    [Fact]
    public async Task CompleteSetupAsync_CreatesChildrenWithPicturePasswords()
    {
        // Arrange
        var setupData = new SetupData
        {
            Admin = new AdminData
            {
                Name = "Admin",
                Email = "admin@test.com",
                Password = "Pass1!"
            },
            Children = new List<ChildData>
            {
                new ChildData
                {
                    Name = "Junior",
                    Birthday = new DateTime(2018, 5, 15),
                    StartingBalance = 10.00m,
                    PictureSequence = new[] { "cat", "dog", "star", "moon" }
                }
            }
        };

        // Act
        var result = await _service.CompleteSetupAsync(setupData);

        // Assert
        Assert.True(result.Success);
        
        var child = Db.Users.FirstOrDefault(u => u.Role == UserRole.Child);
        Assert.NotNull(child);
        Assert.Equal("Junior", child.Name);
        Assert.Equal(new DateTime(2018, 5, 15), child.Birthday);
        Assert.Equal(10.00m, child.Balance);
        Assert.Equal(UserRole.Child, child.Role);
        
        // Verify picture password was created
        var picturePassword = Db.PicturePasswords.FirstOrDefault(p => p.UserId == child.Id);
        Assert.NotNull(picturePassword);
        Assert.Equal(
            SecurityUtils.HashPictureSequence("cat,dog,star,moon"), 
            picturePassword.ImageSequenceHash);
    }

    [Fact]
    public async Task CompleteSetupAsync_CreatesOpeningBalanceTransaction()
    {
        // Arrange
        var setupData = new SetupData
        {
            Admin = new AdminData
            {
                Name = "Admin",
                Email = "admin@test.com",
                Password = "Pass1!"
            },
            Children = new List<ChildData>
            {
                new ChildData
                {
                    Name = "Junior",
                    Birthday = DateTime.Today.AddYears(-5),
                    StartingBalance = 25.00m,
                    PictureSequence = new[] { "cat", "dog", "star", "moon" }
                }
            }
        };

        // Act
        var result = await _service.CompleteSetupAsync(setupData);

        // Assert
        Assert.True(result.Success);
        
        var child = Db.Users.FirstOrDefault(u => u.Role == UserRole.Child);
        Assert.NotNull(child);
        
        var transaction = Db.Transactions.FirstOrDefault(t => t.UserId == child.Id);
        Assert.NotNull(transaction);
        Assert.Equal(25.00m, transaction.Amount);
        Assert.Equal(TransactionType.Deposit, transaction.Type);
        Assert.Equal("Welcome to Juno Bank!", transaction.Description);
        Assert.True(transaction.IsApproved);
    }

    [Fact]
    public async Task CompleteSetupAsync_NoTransactionForZeroBalance()
    {
        // Arrange
        var setupData = new SetupData
        {
            Admin = new AdminData
            {
                Name = "Admin",
                Email = "admin@test.com",
                Password = "Pass1!"
            },
            Children = new List<ChildData>
            {
                new ChildData
                {
                    Name = "Junior",
                    Birthday = DateTime.Today.AddYears(-5),
                    StartingBalance = 0m,
                    PictureSequence = new[] { "cat", "dog", "star", "moon" }
                }
            }
        };

        // Act
        var result = await _service.CompleteSetupAsync(setupData);

        // Assert
        Assert.True(result.Success);
        Assert.Empty(Db.Transactions);
    }

    [Fact]
    public async Task CompleteSetupAsync_HandlesMultipleChildren()
    {
        // Arrange
        var setupData = new SetupData
        {
            Admin = new AdminData
            {
                Name = "Admin",
                Email = "admin@test.com",
                Password = "Pass1!"
            },
            Children = new List<ChildData>
            {
                new ChildData
                {
                    Name = "Junior",
                    Birthday = DateTime.Today.AddYears(-8),
                    StartingBalance = 10.00m,
                    PictureSequence = new[] { "cat", "dog", "star", "moon" }
                },
                new ChildData
                {
                    Name = "Lily",
                    Birthday = DateTime.Today.AddYears(-5),
                    StartingBalance = 5.00m,
                    PictureSequence = new[] { "star", "moon", "cat", "dog" }
                }
            }
        };

        // Act
        var result = await _service.CompleteSetupAsync(setupData);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, Db.Users.Count(u => u.Role == UserRole.Child));
        Assert.Equal(2, Db.PicturePasswords.Count());
        Assert.Equal(2, Db.Transactions.Count());
    }

    #endregion
}
