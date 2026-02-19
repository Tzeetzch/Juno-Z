using JunoBank.Tests.Helpers;
using JunoBank.Web.Data.Entities;
using JunoBank.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace JunoBank.Tests.Services;

public class UserServiceTests : DatabaseTestBase
{
    private readonly UserService _service;

    public UserServiceTests()
    {
        _service = new UserService(Db, CreateLogger<UserService>());
    }

    #region GetAllChildrenSummaryAsync Tests

    [Fact]
    public async Task GetAllChildrenSummaryAsync_ReturnsAllChildren()
    {
        // Arrange
        var child1 = new User { Name = "Junior", Role = UserRole.Child, Balance = 10.00m };
        var child2 = new User { Name = "Sophie", Role = UserRole.Child, Balance = 5.00m };
        var parent = new User { Name = "Dad", Role = UserRole.Parent, Email = "dad@test.com", PasswordHash = "hash" };
        
        Db.Users.AddRange(child1, child2, parent);
        await Db.SaveChangesAsync();

        // Act
        var result = await _service.GetAllChildrenSummaryAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, c => c.Name == "Junior" && c.Balance == 10.00m);
        Assert.Contains(result, c => c.Name == "Sophie" && c.Balance == 5.00m);
    }

    [Fact]
    public async Task GetAllChildrenSummaryAsync_IncludesPendingRequestCount()
    {
        // Arrange
        var child = new User { Name = "Junior", Role = UserRole.Child, Balance = 10.00m };
        Db.Users.Add(child);
        await Db.SaveChangesAsync();

        Db.MoneyRequests.Add(new MoneyRequest
        {
            ChildId = child.Id,
            Amount = 5.00m,
            Type = RequestType.Deposit,
            Description = "Test",
            Status = RequestStatus.Pending
        });
        Db.MoneyRequests.Add(new MoneyRequest
        {
            ChildId = child.Id,
            Amount = 3.00m,
            Type = RequestType.Withdrawal,
            Description = "Test 2",
            Status = RequestStatus.Approved
        });
        await Db.SaveChangesAsync();

        // Act
        var result = await _service.GetAllChildrenSummaryAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal(1, result[0].PendingRequestCount);
    }

    [Fact]
    public async Task GetAllChildrenSummaryAsync_ReturnsEmptyWhenNoChildren()
    {
        // Arrange
        var parent = new User { Name = "Dad", Role = UserRole.Parent, Email = "dad@test.com", PasswordHash = "hash" };
        Db.Users.Add(parent);
        await Db.SaveChangesAsync();

        // Act
        var result = await _service.GetAllChildrenSummaryAsync();

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region GetChildByIdAsync Tests

    [Fact]
    public async Task GetChildByIdAsync_ReturnsChild_WhenExists()
    {
        // Arrange
        var child = new User { Name = "Junior", Role = UserRole.Child, Balance = 10.00m };
        Db.Users.Add(child);
        await Db.SaveChangesAsync();

        // Act
        var result = await _service.GetChildByIdAsync(child.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Junior", result.Name);
        Assert.Equal(10.00m, result.Balance);
    }

    [Fact]
    public async Task GetChildByIdAsync_ReturnsNull_WhenNotFound()
    {
        // Act
        var result = await _service.GetChildByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetChildByIdAsync_ReturnsNull_WhenUserIsParent()
    {
        // Arrange
        var parent = new User { Name = "Dad", Role = UserRole.Parent, Email = "dad@test.com", PasswordHash = "hash" };
        Db.Users.Add(parent);
        await Db.SaveChangesAsync();

        // Act
        var result = await _service.GetChildByIdAsync(parent.Id);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetOpenRequestCountAsync Tests

    [Fact]
    public async Task GetOpenRequestCountAsync_ReturnsPendingCount()
    {
        // Arrange
        var child = new User { Name = "Junior", Role = UserRole.Child, Balance = 10.00m };
        Db.Users.Add(child);
        await Db.SaveChangesAsync();

        Db.MoneyRequests.AddRange(
            new MoneyRequest { ChildId = child.Id, Amount = 1.00m, Type = RequestType.Deposit, Description = "Test 1", Status = RequestStatus.Pending },
            new MoneyRequest { ChildId = child.Id, Amount = 2.00m, Type = RequestType.Deposit, Description = "Test 2", Status = RequestStatus.Pending },
            new MoneyRequest { ChildId = child.Id, Amount = 3.00m, Type = RequestType.Deposit, Description = "Test 3", Status = RequestStatus.Approved },
            new MoneyRequest { ChildId = child.Id, Amount = 4.00m, Type = RequestType.Deposit, Description = "Test 4", Status = RequestStatus.Denied }
        );
        await Db.SaveChangesAsync();

        // Act
        var result = await _service.GetOpenRequestCountAsync(child.Id);

        // Assert
        Assert.Equal(2, result);
    }

    [Fact]
    public async Task GetOpenRequestCountAsync_ReturnsZero_WhenNoPendingRequests()
    {
        // Arrange
        var child = new User { Name = "Junior", Role = UserRole.Child, Balance = 10.00m };
        Db.Users.Add(child);
        await Db.SaveChangesAsync();

        // Act
        var result = await _service.GetOpenRequestCountAsync(child.Id);

        // Assert
        Assert.Equal(0, result);
    }

    #endregion

    #region CreateMoneyRequestAsync - Request Limit Tests

    [Fact]
    public async Task CreateMoneyRequestAsync_ThrowsWhenAtMaxRequests()
    {
        // Arrange
        var child = new User { Name = "Junior", Role = UserRole.Child, Balance = 10.00m };
        Db.Users.Add(child);
        await Db.SaveChangesAsync();

        // Add 5 pending requests (the max)
        for (int i = 1; i <= 5; i++)
        {
            Db.MoneyRequests.Add(new MoneyRequest
            {
                ChildId = child.Id,
                Amount = 1.00m,
                Type = RequestType.Deposit,
                Description = $"Test {i}",
                Status = RequestStatus.Pending
            });
        }
        await Db.SaveChangesAsync();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateMoneyRequestAsync(child.Id, 1.00m, RequestType.Deposit, "This should fail"));

        Assert.Contains("Maximum of 5 pending requests", ex.Message);
    }

    [Fact]
    public async Task CreateMoneyRequestAsync_AllowsRequestWhenUnderLimit()
    {
        // Arrange
        var child = new User { Name = "Junior", Role = UserRole.Child, Balance = 10.00m };
        Db.Users.Add(child);
        await Db.SaveChangesAsync();

        // Add 4 pending requests (under the max of 5)
        for (int i = 1; i <= 4; i++)
        {
            Db.MoneyRequests.Add(new MoneyRequest
            {
                ChildId = child.Id,
                Amount = 1.00m,
                Type = RequestType.Deposit,
                Description = $"Test {i}",
                Status = RequestStatus.Pending
            });
        }
        await Db.SaveChangesAsync();

        // Act
        var result = await _service.CreateMoneyRequestAsync(child.Id, 2.00m, RequestType.Deposit, "Fifth request");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2.00m, result.Amount);
        Assert.Equal(RequestStatus.Pending, result.Status);
    }

    [Fact]
    public async Task CreateMoneyRequestAsync_AllowsRequestWhenApprovedRequestsExist()
    {
        // Arrange
        var child = new User { Name = "Junior", Role = UserRole.Child, Balance = 10.00m };
        Db.Users.Add(child);
        await Db.SaveChangesAsync();

        // Add 5 approved/denied requests (these don't count toward the limit)
        for (int i = 1; i <= 5; i++)
        {
            Db.MoneyRequests.Add(new MoneyRequest
            {
                ChildId = child.Id,
                Amount = 1.00m,
                Type = RequestType.Deposit,
                Description = $"Test {i}",
                Status = i % 2 == 0 ? RequestStatus.Approved : RequestStatus.Denied
            });
        }
        await Db.SaveChangesAsync();

        // Act
        var result = await _service.CreateMoneyRequestAsync(child.Id, 3.00m, RequestType.Deposit, "New request");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3.00m, result.Amount);
    }

    #endregion

    #region User Management (Admin) Tests

    [Fact]
    public async Task GetAllParentsAsync_ReturnsAllParents()
    {
        // Arrange
        var parent1 = new User { Name = "Dad", Email = "dad@test.com", PasswordHash = "hash1", Role = UserRole.Parent, IsAdmin = true };
        var parent2 = new User { Name = "Mom", Email = "mom@test.com", PasswordHash = "hash2", Role = UserRole.Parent, IsAdmin = false };
        var child = new User { Name = "Junior", Role = UserRole.Child };
        
        Db.Users.AddRange(parent1, parent2, child);
        await Db.SaveChangesAsync();

        // Act
        var result = await _service.GetAllParentsAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p.Name == "Dad" && p.IsAdmin);
        Assert.Contains(result, p => p.Name == "Mom" && !p.IsAdmin);
    }

    [Fact]
    public async Task GetAllParentsAsync_ReturnsEmptyWhenNoParents()
    {
        // Arrange
        var child = new User { Name = "Junior", Role = UserRole.Child };
        Db.Users.Add(child);
        await Db.SaveChangesAsync();

        // Act
        var result = await _service.GetAllParentsAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task CreateParentAsync_CreatesParentWithHashedPassword()
    {
        // Act
        var result = await _service.CreateParentAsync("New Parent", "new@test.com", "Password123!", false);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Parent", result.Name);
        Assert.Equal("new@test.com", result.Email);
        Assert.Equal(UserRole.Parent, result.Role);
        Assert.False(result.IsAdmin);
        Assert.True(BCrypt.Net.BCrypt.Verify("Password123!", result.PasswordHash));
    }

    [Fact]
    public async Task CreateParentAsync_SetsAdminFlagCorrectly()
    {
        // Act
        var result = await _service.CreateParentAsync("Admin Parent", "admin@test.com", "Pass!", true);

        // Assert
        Assert.True(result.IsAdmin);
    }

    [Fact]
    public async Task CreateChildAsync_CreatesChildWithPicturePassword()
    {
        // Arrange
        var parent = new User { Name = "Dad", Email = "dad@test.com", PasswordHash = "hash", Role = UserRole.Parent, IsAdmin = true };
        Db.Users.Add(parent);
        await Db.SaveChangesAsync();

        var birthday = new DateTime(2018, 6, 15);
        var pictures = new[] { "cat", "dog", "star", "moon" };

        // Act
        var result = await _service.CreateChildAsync("Junior", birthday, 10.00m, pictures, parent.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Junior", result.Name);
        Assert.Equal(birthday, result.Birthday);
        Assert.Equal(10.00m, result.Balance);
        Assert.Equal(UserRole.Child, result.Role);
        
        // Verify picture password
        var picturePassword = Db.PicturePasswords.FirstOrDefault(p => p.UserId == result.Id);
        Assert.NotNull(picturePassword);
    }

    [Fact]
    public async Task CreateChildAsync_CreatesOpeningBalanceTransaction()
    {
        // Arrange
        var parent = new User { Name = "Dad", Email = "dad@test.com", PasswordHash = "hash", Role = UserRole.Parent, IsAdmin = true };
        Db.Users.Add(parent);
        await Db.SaveChangesAsync();

        // Act
        var result = await _service.CreateChildAsync("Junior", DateTime.Today, 25.00m, new[] { "a", "b", "c", "d" }, parent.Id);

        // Assert
        var transaction = Db.Transactions.FirstOrDefault(t => t.UserId == result.Id);
        Assert.NotNull(transaction);
        Assert.Equal(25.00m, transaction.Amount);
        Assert.Equal(TransactionType.Deposit, transaction.Type);
        Assert.Equal("Opening balance", transaction.Description);
        Assert.True(transaction.IsApproved);
        Assert.Equal(parent.Id, transaction.ApprovedByUserId);
    }

    [Fact]
    public async Task CreateChildAsync_NoTransactionForZeroBalance()
    {
        // Arrange
        var parent = new User { Name = "Dad", Email = "dad@test.com", PasswordHash = "hash", Role = UserRole.Parent, IsAdmin = true };
        Db.Users.Add(parent);
        await Db.SaveChangesAsync();

        // Act
        var result = await _service.CreateChildAsync("Junior", DateTime.Today, 0m, new[] { "a", "b", "c", "d" }, parent.Id);

        // Assert
        Assert.Empty(Db.Transactions);
    }

    [Fact]
    public async Task SetAdminStatusAsync_UpdatesAdminFlag()
    {
        // Arrange
        var admin = new User { Name = "Admin", Email = "admin@test.com", PasswordHash = "hash", Role = UserRole.Parent, IsAdmin = true };
        var parent = new User { Name = "Dad", Email = "dad@test.com", PasswordHash = "hash", Role = UserRole.Parent, IsAdmin = false };
        Db.Users.AddRange(admin, parent);
        await Db.SaveChangesAsync();

        // Act
        await _service.SetAdminStatusAsync(parent.Id, true, admin.Id);

        // Assert
        var updated = await Db.Users.FindAsync(parent.Id);
        Assert.True(updated!.IsAdmin);
    }

    [Fact]
    public async Task SetAdminStatusAsync_IgnoresNonParent()
    {
        // Arrange
        var admin = new User { Name = "Admin", Email = "admin@test.com", PasswordHash = "hash", Role = UserRole.Parent, IsAdmin = true };
        var child = new User { Name = "Junior", Role = UserRole.Child };
        Db.Users.AddRange(admin, child);
        await Db.SaveChangesAsync();

        // Act
        await _service.SetAdminStatusAsync(child.Id, true, admin.Id);

        // Assert
        var updated = await Db.Users.FindAsync(child.Id);
        Assert.False(updated!.IsAdmin);
    }

    [Fact]
    public async Task SetAdminStatusAsync_ThrowsWhenCallerNotAdmin()
    {
        // Arrange
        var nonAdmin = new User { Name = "Mom", Email = "mom@test.com", PasswordHash = "hash", Role = UserRole.Parent, IsAdmin = false };
        var target = new User { Name = "Dad", Email = "dad@test.com", PasswordHash = "hash", Role = UserRole.Parent, IsAdmin = false };
        Db.Users.AddRange(nonAdmin, target);
        await Db.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.SetAdminStatusAsync(target.Id, true, nonAdmin.Id));
    }

    [Fact]
    public async Task CreateParentAsync_ThrowsWhenCallerNotAdmin()
    {
        // Arrange
        var nonAdmin = new User { Name = "Mom", Email = "mom@test.com", PasswordHash = "hash", Role = UserRole.Parent, IsAdmin = false };
        Db.Users.Add(nonAdmin);
        await Db.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.CreateParentAsync("New", "new@test.com", "pass", false, nonAdmin.Id));
    }

    [Fact]
    public async Task CreateChildAsync_ThrowsWhenCallerNotAdmin()
    {
        // Arrange
        var nonAdmin = new User { Name = "Mom", Email = "mom@test.com", PasswordHash = "hash", Role = UserRole.Parent, IsAdmin = false };
        Db.Users.Add(nonAdmin);
        await Db.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.CreateChildAsync("Kid", DateTime.Today, 0, new[] { "a", "b", "c", "d" }, nonAdmin.Id));
    }

    [Fact]
    public async Task IsAdminAsync_ReturnsTrueForAdmin()
    {
        // Arrange
        var parent = new User { Name = "Dad", Email = "dad@test.com", PasswordHash = "hash", Role = UserRole.Parent, IsAdmin = true };
        Db.Users.Add(parent);
        await Db.SaveChangesAsync();

        // Act
        var result = await _service.IsAdminAsync(parent.Id);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsAdminAsync_ReturnsFalseForNonAdmin()
    {
        // Arrange
        var parent = new User { Name = "Dad", Email = "dad@test.com", PasswordHash = "hash", Role = UserRole.Parent, IsAdmin = false };
        Db.Users.Add(parent);
        await Db.SaveChangesAsync();

        // Act
        var result = await _service.IsAdminAsync(parent.Id);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsAdminAsync_ReturnsFalseForNonexistentUser()
    {
        // Act
        var result = await _service.IsAdminAsync(999);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region ResetParentPasswordAsync Tests

    [Fact]
    public async Task ResetParentPasswordAsync_AdminResetsOtherParent_Succeeds()
    {
        // Arrange
        var admin = new User { Name = "Admin", Email = "admin@test.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("oldpass"), Role = UserRole.Parent, IsAdmin = true };
        var target = new User { Name = "Mom", Email = "mom@test.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("oldpass"), Role = UserRole.Parent, IsAdmin = false };
        Db.Users.AddRange(admin, target);
        await Db.SaveChangesAsync();

        // Act
        await _service.ResetParentPasswordAsync(target.Id, "NewPass123", admin.Id);

        // Assert
        var updated = await Db.Users.FindAsync(target.Id);
        Assert.True(BCrypt.Net.BCrypt.Verify("NewPass123", updated!.PasswordHash));
    }

    [Fact]
    public async Task ResetParentPasswordAsync_NonAdminCaller_ThrowsUnauthorized()
    {
        // Arrange
        var nonAdmin = new User { Name = "Mom", Email = "mom@test.com", PasswordHash = "hash", Role = UserRole.Parent, IsAdmin = false };
        var target = new User { Name = "Dad", Email = "dad@test.com", PasswordHash = "hash", Role = UserRole.Parent, IsAdmin = false };
        Db.Users.AddRange(nonAdmin, target);
        await Db.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.ResetParentPasswordAsync(target.Id, "NewPass123", nonAdmin.Id));
    }

    [Fact]
    public async Task ResetParentPasswordAsync_ResetOwnPassword_ThrowsInvalidOperation()
    {
        // Arrange
        var admin = new User { Name = "Admin", Email = "admin@test.com", PasswordHash = "hash", Role = UserRole.Parent, IsAdmin = true };
        Db.Users.Add(admin);
        await Db.SaveChangesAsync();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ResetParentPasswordAsync(admin.Id, "NewPass123", admin.Id));
        Assert.Contains("Cannot reset your own password", ex.Message);
    }

    [Fact]
    public async Task ResetParentPasswordAsync_TargetNotFound_ThrowsArgument()
    {
        // Arrange
        var admin = new User { Name = "Admin", Email = "admin@test.com", PasswordHash = "hash", Role = UserRole.Parent, IsAdmin = true };
        Db.Users.Add(admin);
        await Db.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.ResetParentPasswordAsync(999, "NewPass123", admin.Id));
    }

    [Fact]
    public async Task ResetParentPasswordAsync_TargetIsChild_ThrowsArgument()
    {
        // Arrange
        var admin = new User { Name = "Admin", Email = "admin@test.com", PasswordHash = "hash", Role = UserRole.Parent, IsAdmin = true };
        var child = new User { Name = "Junior", Role = UserRole.Child };
        Db.Users.AddRange(admin, child);
        await Db.SaveChangesAsync();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.ResetParentPasswordAsync(child.Id, "NewPass123", admin.Id));
        Assert.Contains("parent accounts", ex.Message);
    }

    [Fact]
    public async Task ResetParentPasswordAsync_ShortPassword_ThrowsArgument()
    {
        // Arrange
        var admin = new User { Name = "Admin", Email = "admin@test.com", PasswordHash = "hash", Role = UserRole.Parent, IsAdmin = true };
        var target = new User { Name = "Mom", Email = "mom@test.com", PasswordHash = "hash", Role = UserRole.Parent, IsAdmin = false };
        Db.Users.AddRange(admin, target);
        await Db.SaveChangesAsync();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.ResetParentPasswordAsync(target.Id, "short", admin.Id));
        Assert.Contains("at least 8 characters", ex.Message);
    }

    [Fact]
    public async Task ResetParentPasswordAsync_ClearsLockoutState()
    {
        // Arrange
        var admin = new User { Name = "Admin", Email = "admin@test.com", PasswordHash = "hash", Role = UserRole.Parent, IsAdmin = true };
        var target = new User
        {
            Name = "Mom", Email = "mom@test.com", PasswordHash = "hash", Role = UserRole.Parent, IsAdmin = false,
            FailedLoginAttempts = 5, LockoutUntil = DateTime.UtcNow.AddMinutes(10)
        };
        Db.Users.AddRange(admin, target);
        await Db.SaveChangesAsync();

        // Act
        await _service.ResetParentPasswordAsync(target.Id, "NewPass123", admin.Id);

        // Assert
        var updated = await Db.Users.FindAsync(target.Id);
        Assert.Equal(0, updated!.FailedLoginAttempts);
        Assert.Null(updated.LockoutUntil);
    }

    #endregion

    #region Pagination Tests

    [Fact]
    public async Task GetTransactionsForChildAsync_RespectsSkipAndLimit()
    {
        // Arrange
        var child = new User { Name = "Junior", Role = UserRole.Child, Balance = 100m };
        Db.Users.Add(child);
        await Db.SaveChangesAsync();

        for (int i = 0; i < 5; i++)
        {
            Db.Transactions.Add(new Transaction
            {
                UserId = child.Id,
                Amount = (i + 1) * 10m,
                Type = TransactionType.Deposit,
                Description = $"Tx {i}",
                IsApproved = true,
                CreatedAt = DateTime.UtcNow.AddMinutes(-i)
            });
        }
        await Db.SaveChangesAsync();

        // Act - get first page of 2
        var page1 = await _service.GetTransactionsForChildAsync(child.Id, skip: 0, limit: 2);
        // Act - get second page of 2
        var page2 = await _service.GetTransactionsForChildAsync(child.Id, skip: 2, limit: 2);
        // Act - get third page (only 1 left)
        var page3 = await _service.GetTransactionsForChildAsync(child.Id, skip: 4, limit: 2);

        // Assert
        Assert.Equal(2, page1.Count);
        Assert.Equal(2, page2.Count);
        Assert.Single(page3);
        // Pages should not overlap
        Assert.DoesNotContain(page1, t => page2.Any(t2 => t2.Id == t.Id));
    }

    [Fact]
    public async Task GetCompletedRequestsForChildAsync_RespectsSkipAndLimit()
    {
        // Arrange
        var child = new User { Name = "Junior", Role = UserRole.Child, Balance = 100m };
        Db.Users.Add(child);
        await Db.SaveChangesAsync();

        for (int i = 0; i < 5; i++)
        {
            Db.MoneyRequests.Add(new MoneyRequest
            {
                ChildId = child.Id,
                Amount = (i + 1) * 5m,
                Type = RequestType.Deposit,
                Description = $"Req {i}",
                Status = RequestStatus.Approved,
                ResolvedAt = DateTime.UtcNow.AddMinutes(-i)
            });
        }
        await Db.SaveChangesAsync();

        // Act
        var page1 = await _service.GetCompletedRequestsForChildAsync(child.Id, skip: 0, limit: 3);
        var page2 = await _service.GetCompletedRequestsForChildAsync(child.Id, skip: 3, limit: 3);

        // Assert
        Assert.Equal(3, page1.Count);
        Assert.Equal(2, page2.Count);
        Assert.DoesNotContain(page1, r => page2.Any(r2 => r2.Id == r.Id));
    }

    [Fact]
    public async Task GetTransactionsForChildAsync_ReturnsEmpty_WhenSkipExceedsTotal()
    {
        // Arrange
        var child = new User { Name = "Junior", Role = UserRole.Child, Balance = 10m };
        Db.Users.Add(child);
        await Db.SaveChangesAsync();

        Db.Transactions.Add(new Transaction
        {
            UserId = child.Id,
            Amount = 10m,
            Type = TransactionType.Deposit,
            Description = "Single",
            IsApproved = true
        });
        await Db.SaveChangesAsync();

        // Act
        var result = await _service.GetTransactionsForChildAsync(child.Id, skip: 100, limit: 20);

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region UpdatePicturePasswordAsync Tests

    [Fact]
    public async Task UpdatePicturePasswordAsync_UpdatesHash()
    {
        // Arrange
        var child = new User { Name = "Junior", Role = UserRole.Child };
        Db.Users.Add(child);
        await Db.SaveChangesAsync();

        Db.PicturePasswords.Add(new PicturePassword
        {
            UserId = child.Id,
            ImageSequenceHash = "oldhash",
            GridSize = 9,
            SequenceLength = 4
        });
        await Db.SaveChangesAsync();

        // Act
        await _service.UpdatePicturePasswordAsync(child.Id, new[] { "star", "moon", "cat", "dog" });

        // Assert
        var updated = await Db.PicturePasswords.FirstAsync(p => p.UserId == child.Id);
        Assert.NotEqual("oldhash", updated.ImageSequenceHash);
        Assert.Equal(Web.Utils.SecurityUtils.HashPictureSequence("star,moon,cat,dog"), updated.ImageSequenceHash);
    }

    [Fact]
    public async Task UpdatePicturePasswordAsync_ClearsLockoutState()
    {
        // Arrange
        var child = new User { Name = "Junior", Role = UserRole.Child };
        Db.Users.Add(child);
        await Db.SaveChangesAsync();

        Db.PicturePasswords.Add(new PicturePassword
        {
            UserId = child.Id,
            ImageSequenceHash = "oldhash",
            FailedAttempts = 5,
            LockedUntil = DateTime.UtcNow.AddMinutes(5)
        });
        await Db.SaveChangesAsync();

        // Act
        await _service.UpdatePicturePasswordAsync(child.Id, new[] { "a", "b", "c", "d" });

        // Assert
        var updated = await Db.PicturePasswords.FirstAsync(p => p.UserId == child.Id);
        Assert.Equal(0, updated.FailedAttempts);
        Assert.Null(updated.LockedUntil);
    }

    [Fact]
    public async Task UpdatePicturePasswordAsync_WrongLength_ThrowsArgument()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.UpdatePicturePasswordAsync(1, new[] { "a", "b" }));
    }

    [Fact]
    public async Task UpdatePicturePasswordAsync_ChildNotFound_ThrowsArgument()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.UpdatePicturePasswordAsync(999, new[] { "a", "b", "c", "d" }));
    }

    #endregion

    #region ConcurrencyStamp Tests

    [Fact]
    public async Task CreateManualTransactionAsync_IncrementsConcurrencyStamp()
    {
        // Arrange
        var parent = new User { Name = "Dad", Email = "dad@test.com", PasswordHash = "hash", Role = UserRole.Parent };
        var child = new User { Name = "Junior", Role = UserRole.Child, Balance = 50m, ConcurrencyStamp = 0 };
        Db.Users.AddRange(parent, child);
        await Db.SaveChangesAsync();

        // Act
        await _service.CreateManualTransactionAsync(parent.Id, 10m, TransactionType.Deposit, "Gift");

        // Assert
        var updated = await Db.Users.FindAsync(child.Id);
        Assert.Equal(1, updated!.ConcurrencyStamp);
    }

    [Fact]
    public async Task CreateManualTransactionForChildAsync_IncrementsConcurrencyStamp()
    {
        // Arrange
        var parent = new User { Name = "Dad", Email = "dad@test.com", PasswordHash = "hash", Role = UserRole.Parent };
        var child = new User { Name = "Junior", Role = UserRole.Child, Balance = 50m, ConcurrencyStamp = 0 };
        Db.Users.AddRange(parent, child);
        await Db.SaveChangesAsync();

        // Act
        await _service.CreateManualTransactionForChildAsync(parent.Id, child.Id, 10m, TransactionType.Deposit, "Gift");

        // Assert
        var updated = await Db.Users.FindAsync(child.Id);
        Assert.Equal(1, updated!.ConcurrencyStamp);
    }

    [Fact]
    public async Task ResolveRequestAsync_Approve_IncrementsConcurrencyStamp()
    {
        // Arrange
        var parent = new User { Name = "Dad", Email = "dad@test.com", PasswordHash = "hash", Role = UserRole.Parent };
        var child = new User { Name = "Junior", Role = UserRole.Child, Balance = 50m, ConcurrencyStamp = 0 };
        Db.Users.AddRange(parent, child);
        await Db.SaveChangesAsync();

        var request = new MoneyRequest
        {
            ChildId = child.Id,
            Amount = 5m,
            Type = RequestType.Deposit,
            Description = "Please",
            Status = RequestStatus.Pending
        };
        Db.MoneyRequests.Add(request);
        await Db.SaveChangesAsync();

        // Act
        await _service.ResolveRequestAsync(request.Id, parent.Id, approve: true);

        // Assert
        var updated = await Db.Users.FindAsync(child.Id);
        Assert.Equal(1, updated!.ConcurrencyStamp);
    }

    [Fact]
    public async Task ResolveRequestAsync_Deny_DoesNotIncrementConcurrencyStamp()
    {
        // Arrange
        var parent = new User { Name = "Dad", Email = "dad@test.com", PasswordHash = "hash", Role = UserRole.Parent };
        var child = new User { Name = "Junior", Role = UserRole.Child, Balance = 50m, ConcurrencyStamp = 0 };
        Db.Users.AddRange(parent, child);
        await Db.SaveChangesAsync();

        var request = new MoneyRequest
        {
            ChildId = child.Id,
            Amount = 5m,
            Type = RequestType.Deposit,
            Description = "Please",
            Status = RequestStatus.Pending
        };
        Db.MoneyRequests.Add(request);
        await Db.SaveChangesAsync();

        // Act
        await _service.ResolveRequestAsync(request.Id, parent.Id, approve: false);

        // Assert
        var updated = await Db.Users.FindAsync(child.Id);
        Assert.Equal(0, updated!.ConcurrencyStamp);
    }

    [Fact]
    public async Task MultipleTransactions_IncrementsConcurrencyStampEachTime()
    {
        // Arrange
        var parent = new User { Name = "Dad", Email = "dad@test.com", PasswordHash = "hash", Role = UserRole.Parent };
        var child = new User { Name = "Junior", Role = UserRole.Child, Balance = 100m, ConcurrencyStamp = 0 };
        Db.Users.AddRange(parent, child);
        await Db.SaveChangesAsync();

        // Act
        await _service.CreateManualTransactionForChildAsync(parent.Id, child.Id, 10m, TransactionType.Deposit, "Gift 1");
        await _service.CreateManualTransactionForChildAsync(parent.Id, child.Id, 5m, TransactionType.Withdrawal, "Spent");
        await _service.CreateManualTransactionForChildAsync(parent.Id, child.Id, 20m, TransactionType.Deposit, "Gift 2");

        // Assert
        var updated = await Db.Users.FindAsync(child.Id);
        Assert.Equal(3, updated!.ConcurrencyStamp);
        Assert.Equal(125m, updated.Balance); // 100 + 10 - 5 + 20
    }

    #endregion

    #region UnlockChildAsync Tests

    [Fact]
    public async Task UnlockChildAsync_ClearsLockoutState()
    {
        // Arrange
        var child = new User { Name = "Junior", Role = UserRole.Child };
        Db.Users.Add(child);
        await Db.SaveChangesAsync();

        Db.PicturePasswords.Add(new PicturePassword
        {
            UserId = child.Id,
            ImageSequenceHash = "hash",
            FailedAttempts = 5,
            LockedUntil = DateTime.UtcNow.AddMinutes(5)
        });
        await Db.SaveChangesAsync();

        // Act
        await _service.UnlockChildAsync(child.Id);

        // Assert
        var updated = await Db.PicturePasswords.FirstAsync(p => p.UserId == child.Id);
        Assert.Equal(0, updated.FailedAttempts);
        Assert.Null(updated.LockedUntil);
    }

    [Fact]
    public async Task UnlockChildAsync_ChildNotFound_ThrowsArgument()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.UnlockChildAsync(999));
    }

    #endregion

    #region GetChildLockoutStatusAsync Tests

    [Fact]
    public async Task GetChildLockoutStatusAsync_LockedChild_ReturnsLocked()
    {
        // Arrange
        var child = new User { Name = "Junior", Role = UserRole.Child };
        Db.Users.Add(child);
        await Db.SaveChangesAsync();

        var lockedUntil = DateTime.UtcNow.AddMinutes(5);
        Db.PicturePasswords.Add(new PicturePassword
        {
            UserId = child.Id,
            ImageSequenceHash = "hash",
            FailedAttempts = 5,
            LockedUntil = lockedUntil
        });
        await Db.SaveChangesAsync();

        // Act
        var status = await _service.GetChildLockoutStatusAsync(child.Id);

        // Assert
        Assert.True(status.IsLocked);
        Assert.NotNull(status.LockedUntil);
        Assert.Equal(5, status.FailedAttempts);
    }

    [Fact]
    public async Task GetChildLockoutStatusAsync_UnlockedChild_ReturnsNotLocked()
    {
        // Arrange
        var child = new User { Name = "Junior", Role = UserRole.Child };
        Db.Users.Add(child);
        await Db.SaveChangesAsync();

        Db.PicturePasswords.Add(new PicturePassword
        {
            UserId = child.Id,
            ImageSequenceHash = "hash",
            FailedAttempts = 0
        });
        await Db.SaveChangesAsync();

        // Act
        var status = await _service.GetChildLockoutStatusAsync(child.Id);

        // Assert
        Assert.False(status.IsLocked);
        Assert.Null(status.LockedUntil);
        Assert.Equal(0, status.FailedAttempts);
    }

    [Fact]
    public async Task GetChildLockoutStatusAsync_ExpiredLockout_ReturnsNotLocked()
    {
        // Arrange
        var child = new User { Name = "Junior", Role = UserRole.Child };
        Db.Users.Add(child);
        await Db.SaveChangesAsync();

        Db.PicturePasswords.Add(new PicturePassword
        {
            UserId = child.Id,
            ImageSequenceHash = "hash",
            FailedAttempts = 5,
            LockedUntil = DateTime.UtcNow.AddMinutes(-1) // Expired
        });
        await Db.SaveChangesAsync();

        // Act
        var status = await _service.GetChildLockoutStatusAsync(child.Id);

        // Assert
        Assert.False(status.IsLocked);
    }

    [Fact]
    public async Task GetChildLockoutStatusAsync_ChildNotFound_ReturnsDefault()
    {
        // Act
        var status = await _service.GetChildLockoutStatusAsync(999);

        // Assert
        Assert.False(status.IsLocked);
        Assert.Equal(0, status.FailedAttempts);
    }

    #endregion
}
