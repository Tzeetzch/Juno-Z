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
}
