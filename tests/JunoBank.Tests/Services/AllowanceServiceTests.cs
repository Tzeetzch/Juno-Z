using JunoBank.Tests.Helpers;
using JunoBank.Web.Data.Entities;
using JunoBank.Web.Services;
using Microsoft.Extensions.Time.Testing;

namespace JunoBank.Tests.Services;

public class AllowanceServiceTests : DatabaseTestBase
{
    private readonly FakeTimeProvider _timeProvider;
    private readonly AllowanceService _service;

    public AllowanceServiceTests()
    {
        // Start at Thursday, Jan 15 2026, 10:00 AM
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 1, 15, 10, 0, 0, TimeSpan.Zero));
        _service = new AllowanceService(Db, _timeProvider, CreateLogger<AllowanceService>());
    }

    private DateTime Now => _timeProvider.GetLocalNow().DateTime;

    #region CalculateNextRunDate Tests

    [Fact]
    public void CalculateNextRunDate_SameDayBeforeTime_ReturnsTodayAtTime()
    {
        // Thursday 10:00, allowance set for Thursday 14:00
        var result = _service.CalculateNextRunDate(
            DayOfWeek.Thursday,
            new TimeOnly(14, 0),
            Now);

        Assert.Equal(new DateTime(2026, 1, 15, 14, 0, 0), result);
    }

    [Fact]
    public void CalculateNextRunDate_SameDayAfterTime_ReturnsNextWeek()
    {
        // Thursday 10:00, allowance set for Thursday 9:00 (already passed)
        var result = _service.CalculateNextRunDate(
            DayOfWeek.Thursday,
            new TimeOnly(9, 0),
            Now);

        // Should be next Thursday
        Assert.Equal(new DateTime(2026, 1, 22, 9, 0, 0), result);
    }

    [Fact]
    public void CalculateNextRunDate_FutureDay_ReturnsCorrectDate()
    {
        // Thursday 10:00, allowance set for Saturday 9:00
        var result = _service.CalculateNextRunDate(
            DayOfWeek.Saturday,
            new TimeOnly(9, 0),
            Now);

        // Saturday is 2 days away (Jan 17)
        Assert.Equal(new DateTime(2026, 1, 17, 9, 0, 0), result);
    }

    [Fact]
    public void CalculateNextRunDate_PastDay_ReturnsNextWeek()
    {
        // Thursday 10:00, allowance set for Monday 9:00 (3 days ago)
        var result = _service.CalculateNextRunDate(
            DayOfWeek.Monday,
            new TimeOnly(9, 0),
            Now);

        // Next Monday is 4 days away (Jan 19)
        Assert.Equal(new DateTime(2026, 1, 19, 9, 0, 0), result);
    }

    #endregion

    #region ProcessDueAllowancesAsync Tests

    [Fact]
    public async Task ProcessDueAllowancesAsync_NoAllowances_ReturnsZero()
    {
        var count = await _service.ProcessDueAllowancesAsync();

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task ProcessDueAllowancesAsync_InactiveAllowance_ReturnsZero()
    {
        // Setup: create child and inactive allowance
        var child = new User { Name = "Junior", Role = UserRole.Child, Balance = 10m };
        Db.Users.Add(child);
        await Db.SaveChangesAsync();

        var allowance = new ScheduledAllowance
        {
            ChildId = child.Id,
            Amount = 5m,
            DayOfWeek = DayOfWeek.Saturday,
            TimeOfDay = new TimeOnly(9, 0),
            Description = "Weekly Allowance",
            IsActive = false, // Inactive
            NextRunDate = Now.AddDays(-1) // Past due
        };
        Db.ScheduledAllowances.Add(allowance);
        await Db.SaveChangesAsync();

        var count = await _service.ProcessDueAllowancesAsync();

        Assert.Equal(0, count);
        Assert.Equal(10m, child.Balance); // Balance unchanged
    }

    [Fact]
    public async Task ProcessDueAllowancesAsync_FutureAllowance_ReturnsZero()
    {
        // Setup: create child and future allowance
        var child = new User { Name = "Junior", Role = UserRole.Child, Balance = 10m };
        Db.Users.Add(child);
        await Db.SaveChangesAsync();

        var allowance = new ScheduledAllowance
        {
            ChildId = child.Id,
            Amount = 5m,
            DayOfWeek = DayOfWeek.Saturday,
            TimeOfDay = new TimeOnly(9, 0),
            Description = "Weekly Allowance",
            IsActive = true,
            NextRunDate = Now.AddDays(3) // Future
        };
        Db.ScheduledAllowances.Add(allowance);
        await Db.SaveChangesAsync();

        var count = await _service.ProcessDueAllowancesAsync();

        Assert.Equal(0, count);
        Assert.Equal(10m, child.Balance); // Balance unchanged
    }

    [Fact]
    public async Task ProcessDueAllowancesAsync_DueAllowance_CreatesTransactionAndUpdatesBalance()
    {
        // Setup: create child and due allowance
        var child = new User { Name = "Junior", Role = UserRole.Child, Balance = 10m };
        Db.Users.Add(child);
        await Db.SaveChangesAsync();

        var scheduledTime = Now.AddMinutes(-5); // 5 minutes ago
        var allowance = new ScheduledAllowance
        {
            ChildId = child.Id,
            Amount = 5m,
            DayOfWeek = DayOfWeek.Wednesday,
            TimeOfDay = new TimeOnly(9, 55),
            Description = "Weekly Allowance",
            IsActive = true,
            NextRunDate = scheduledTime
        };
        Db.ScheduledAllowances.Add(allowance);
        await Db.SaveChangesAsync();

        var count = await _service.ProcessDueAllowancesAsync();

        // Verify
        Assert.Equal(1, count);
        Assert.Equal(15m, child.Balance); // 10 + 5

        // Check transaction was created
        var transaction = Db.Transactions.FirstOrDefault();
        Assert.NotNull(transaction);
        Assert.Equal(5m, transaction.Amount);
        Assert.Equal(TransactionType.Allowance, transaction.Type);
        Assert.Equal("Weekly Allowance", transaction.Description);
        Assert.True(transaction.IsApproved);
        Assert.Null(transaction.ApprovedByUserId); // System-generated

        // Check next run date was updated
        Assert.True(allowance.NextRunDate > Now);
        Assert.Equal(scheduledTime, allowance.LastRunDate);
    }

    [Fact]
    public async Task ProcessDueAllowancesAsync_MissedMultipleWeeks_CatchesUp()
    {
        // Setup: server was offline for 3 weeks
        var child = new User { Name = "Junior", Role = UserRole.Child, Balance = 10m };
        Db.Users.Add(child);
        await Db.SaveChangesAsync();

        // Allowance was due 3 weeks ago (missed 3 payments)
        var threeWeeksAgo = Now.AddDays(-21);
        var allowance = new ScheduledAllowance
        {
            ChildId = child.Id,
            Amount = 5m,
            DayOfWeek = threeWeeksAgo.DayOfWeek,
            TimeOfDay = TimeOnly.FromDateTime(threeWeeksAgo),
            Description = "Weekly Allowance",
            IsActive = true,
            NextRunDate = threeWeeksAgo
        };
        Db.ScheduledAllowances.Add(allowance);
        await Db.SaveChangesAsync();

        var count = await _service.ProcessDueAllowancesAsync();

        // Should have caught up all 3 missed + 1 current week = 4 payments
        // (3 weeks ago, 2 weeks ago, 1 week ago, this week if past time)
        Assert.True(count >= 3, $"Expected at least 3 catch-up payments, got {count}");
        Assert.True(child.Balance >= 25m, $"Expected balance >= 25, got {child.Balance}"); // 10 + (3 * 5) minimum

        // Verify transactions were created
        var transactions = Db.Transactions.ToList();
        Assert.True(transactions.Count >= 3);

        // Next run should be in the future
        Assert.True(allowance.NextRunDate > Now);
    }

    #endregion

    #region UpdateAllowanceAsync Tests

    [Fact]
    public async Task UpdateAllowanceAsync_NewAllowance_CreatesRecord()
    {
        // Setup: create parent and child
        var parent = new User { Name = "Dad", Role = UserRole.Parent, Email = "dad@test.com" };
        var child = new User { Name = "Junior", Role = UserRole.Child, Balance = 10m };
        Db.Users.AddRange(parent, child);
        await Db.SaveChangesAsync();

        await _service.UpdateAllowanceAsync(
            parent.Id,
            amount: 5m,
            dayOfWeek: DayOfWeek.Saturday,
            timeOfDay: new TimeOnly(9, 0),
            description: "Pocket money",
            isActive: true);

        var allowance = await _service.GetAllowanceAsync();
        Assert.NotNull(allowance);
        Assert.Equal(5m, allowance.Amount);
        Assert.Equal(DayOfWeek.Saturday, allowance.DayOfWeek);
        Assert.Equal(new TimeOnly(9, 0), allowance.TimeOfDay);
        Assert.Equal("Pocket money", allowance.Description);
        Assert.True(allowance.IsActive);
        Assert.True(allowance.NextRunDate > Now);
    }

    [Fact]
    public async Task UpdateAllowanceAsync_ExistingAllowance_Updates()
    {
        // Setup: create parent, child, and existing allowance
        var parent = new User { Name = "Dad", Role = UserRole.Parent, Email = "dad@test.com" };
        var child = new User { Name = "Junior", Role = UserRole.Child, Balance = 10m };
        Db.Users.AddRange(parent, child);
        await Db.SaveChangesAsync();

        var existing = new ScheduledAllowance
        {
            ChildId = child.Id,
            CreatedByUserId = parent.Id,
            Amount = 3m,
            DayOfWeek = DayOfWeek.Monday,
            TimeOfDay = new TimeOnly(8, 0),
            Description = "Old description",
            IsActive = true,
            NextRunDate = Now.AddDays(5)
        };
        Db.ScheduledAllowances.Add(existing);
        await Db.SaveChangesAsync();

        // Update
        await _service.UpdateAllowanceAsync(
            parent.Id,
            amount: 10m,
            dayOfWeek: DayOfWeek.Friday,
            timeOfDay: new TimeOnly(15, 0),
            description: "New description",
            isActive: true);

        var allowance = await _service.GetAllowanceAsync();
        Assert.NotNull(allowance);
        Assert.Equal(10m, allowance.Amount);
        Assert.Equal(DayOfWeek.Friday, allowance.DayOfWeek);
        Assert.Equal(new TimeOnly(15, 0), allowance.TimeOfDay);
        Assert.Equal("New description", allowance.Description);

        // Only one record should exist
        Assert.Equal(1, Db.ScheduledAllowances.Count());
    }

    [Fact]
    public async Task UpdateAllowanceAsync_Deactivate_StopsProcessing()
    {
        // Setup
        var parent = new User { Name = "Dad", Role = UserRole.Parent, Email = "dad@test.com" };
        var child = new User { Name = "Junior", Role = UserRole.Child, Balance = 10m };
        Db.Users.AddRange(parent, child);
        await Db.SaveChangesAsync();

        // Create and activate
        await _service.UpdateAllowanceAsync(parent.Id, 5m, DayOfWeek.Saturday, new TimeOnly(9, 0), "Test", true);

        // Deactivate
        await _service.UpdateAllowanceAsync(parent.Id, 5m, DayOfWeek.Saturday, new TimeOnly(9, 0), "Test", false);

        var allowance = await _service.GetAllowanceAsync();
        Assert.NotNull(allowance);
        Assert.False(allowance.IsActive);
    }

    #endregion

    #region GetNextRunDateAsync Tests

    [Fact]
    public async Task GetNextRunDateAsync_NoAllowance_ReturnsNull()
    {
        var result = await _service.GetNextRunDateAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task GetNextRunDateAsync_InactiveAllowance_ReturnsNull()
    {
        var child = new User { Name = "Junior", Role = UserRole.Child };
        Db.Users.Add(child);
        await Db.SaveChangesAsync();

        Db.ScheduledAllowances.Add(new ScheduledAllowance
        {
            ChildId = child.Id,
            IsActive = false,
            NextRunDate = Now.AddDays(1)
        });
        await Db.SaveChangesAsync();

        var result = await _service.GetNextRunDateAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task GetNextRunDateAsync_ActiveAllowance_ReturnsDate()
    {
        var child = new User { Name = "Junior", Role = UserRole.Child };
        Db.Users.Add(child);
        await Db.SaveChangesAsync();

        var nextRun = Now.AddDays(3);
        Db.ScheduledAllowances.Add(new ScheduledAllowance
        {
            ChildId = child.Id,
            IsActive = true,
            NextRunDate = nextRun
        });
        await Db.SaveChangesAsync();

        var result = await _service.GetNextRunDateAsync();

        Assert.Equal(nextRun, result);
    }

    #endregion
}

