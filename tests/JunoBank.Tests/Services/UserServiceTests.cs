using JunoBank.Tests.Helpers;
using JunoBank.Web.Data.Entities;
using JunoBank.Web.Services;

namespace JunoBank.Tests.Services;

public class UserServiceTests : DatabaseTestBase
{
    private readonly UserService _service;

    public UserServiceTests()
    {
        _service = new UserService(Db);
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
        var parent = new User { Name = "Dad", Email = "dad@test.com", PasswordHash = "hash", Role = UserRole.Parent };
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
        var parent = new User { Name = "Dad", Email = "dad@test.com", PasswordHash = "hash", Role = UserRole.Parent, IsAdmin = false };
        Db.Users.Add(parent);
        await Db.SaveChangesAsync();

        // Act
        await _service.SetAdminStatusAsync(parent.Id, true);

        // Assert
        var updated = await Db.Users.FindAsync(parent.Id);
        Assert.True(updated!.IsAdmin);
    }

    [Fact]
    public async Task SetAdminStatusAsync_IgnoresNonParent()
    {
        // Arrange
        var child = new User { Name = "Junior", Role = UserRole.Child };
        Db.Users.Add(child);
        await Db.SaveChangesAsync();

        // Act
        await _service.SetAdminStatusAsync(child.Id, true);

        // Assert
        var updated = await Db.Users.FindAsync(child.Id);
        Assert.False(updated!.IsAdmin);
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
}
