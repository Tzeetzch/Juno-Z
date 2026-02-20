using JunoBank.Tests.Helpers;
using JunoBank.Domain.Entities;
using JunoBank.Domain.Enums;
using JunoBank.Application.Services;
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

    private DateTime Now => _timeProvider.GetUtcNow().DateTime;

    #region CalculateNextRunDate Tests

    [Fact]
    public void CalculateNextRunDate_Weekly_SameDayBeforeTime_ReturnsTodayAtTime()
    {
        // Thursday 10:00, allowance set for Thursday 14:00
        var result = _service.CalculateNextRunDate(
            AllowanceInterval.Weekly,
            DayOfWeek.Thursday,
            1, 1, // dayOfMonth, monthOfYear (not used for weekly)
            new TimeOnly(14, 0),
            Now);

        Assert.Equal(new DateTime(2026, 1, 15, 14, 0, 0), result);
    }

    [Fact]
    public void CalculateNextRunDate_Weekly_SameDayAfterTime_ReturnsNextWeek()
    {
        // Thursday 10:00, allowance set for Thursday 9:00 (already passed)
        var result = _service.CalculateNextRunDate(
            AllowanceInterval.Weekly,
            DayOfWeek.Thursday,
            1, 1,
            new TimeOnly(9, 0),
            Now);

        // Should be next Thursday
        Assert.Equal(new DateTime(2026, 1, 22, 9, 0, 0), result);
    }

    [Fact]
    public void CalculateNextRunDate_Weekly_FutureDay_ReturnsCorrectDate()
    {
        // Thursday 10:00, allowance set for Saturday 9:00
        var result = _service.CalculateNextRunDate(
            AllowanceInterval.Weekly,
            DayOfWeek.Saturday,
            1, 1,
            new TimeOnly(9, 0),
            Now);

        // Saturday is 2 days away (Jan 17)
        Assert.Equal(new DateTime(2026, 1, 17, 9, 0, 0), result);
    }

    [Fact]
    public void CalculateNextRunDate_Weekly_PastDay_ReturnsNextWeek()
    {
        // Thursday 10:00, allowance set for Monday 9:00 (3 days ago)
        var result = _service.CalculateNextRunDate(
            AllowanceInterval.Weekly,
            DayOfWeek.Monday,
            1, 1,
            new TimeOnly(9, 0),
            Now);

        // Next Monday is 4 days away (Jan 19)
        Assert.Equal(new DateTime(2026, 1, 19, 9, 0, 0), result);
    }

    [Fact]
    public void CalculateNextRunDate_Daily_BeforeTime_ReturnsToday()
    {
        // Thursday 10:00, daily at 14:00
        var result = _service.CalculateNextRunDate(
            AllowanceInterval.Daily,
            DayOfWeek.Thursday, 1, 1, // not used for daily
            new TimeOnly(14, 0),
            Now);

        Assert.Equal(new DateTime(2026, 1, 15, 14, 0, 0), result);
    }

    [Fact]
    public void CalculateNextRunDate_Daily_AfterTime_ReturnsTomorrow()
    {
        // Thursday 10:00, daily at 9:00 (already passed)
        var result = _service.CalculateNextRunDate(
            AllowanceInterval.Daily,
            DayOfWeek.Thursday, 1, 1,
            new TimeOnly(9, 0),
            Now);

        Assert.Equal(new DateTime(2026, 1, 16, 9, 0, 0), result);
    }

    [Fact]
    public void CalculateNextRunDate_Hourly_ReturnsNextHour()
    {
        // Thursday 10:00
        var result = _service.CalculateNextRunDate(
            AllowanceInterval.Hourly,
            DayOfWeek.Thursday, 1, 1,
            new TimeOnly(9, 0), // not used
            Now);

        Assert.Equal(new DateTime(2026, 1, 15, 11, 0, 0), result);
    }

    [Fact]
    public void CalculateNextRunDate_Monthly_BeforeDay_ReturnsThisMonth()
    {
        // Jan 15, monthly on 20th at 9:00
        var result = _service.CalculateNextRunDate(
            AllowanceInterval.Monthly,
            DayOfWeek.Thursday, 20, 1,
            new TimeOnly(9, 0),
            Now);

        Assert.Equal(new DateTime(2026, 1, 20, 9, 0, 0), result);
    }

    [Fact]
    public void CalculateNextRunDate_Monthly_AfterDay_ReturnsNextMonth()
    {
        // Jan 15, monthly on 10th at 9:00 (already passed)
        var result = _service.CalculateNextRunDate(
            AllowanceInterval.Monthly,
            DayOfWeek.Thursday, 10, 1,
            new TimeOnly(9, 0),
            Now);

        Assert.Equal(new DateTime(2026, 2, 10, 9, 0, 0), result);
    }

    [Fact]
    public void CalculateNextRunDate_Yearly_BeforeDate_ReturnsThisYear()
    {
        // Jan 15, yearly on March 1st at 9:00
        var result = _service.CalculateNextRunDate(
            AllowanceInterval.Yearly,
            DayOfWeek.Thursday, 1, 3, // March 1
            new TimeOnly(9, 0),
            Now);

        Assert.Equal(new DateTime(2026, 3, 1, 9, 0, 0), result);
    }

    [Fact]
    public void CalculateNextRunDate_Yearly_AfterDate_ReturnsNextYear()
    {
        // Jan 15, yearly on Jan 1st at 9:00 (already passed)
        var result = _service.CalculateNextRunDate(
            AllowanceInterval.Yearly,
            DayOfWeek.Thursday, 1, 1, // January 1
            new TimeOnly(9, 0),
            Now);

        Assert.Equal(new DateTime(2027, 1, 1, 9, 0, 0), result);
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

    [Fact]
    public async Task ProcessDueAllowancesAsync_IncrementsConcurrencyStamp()
    {
        // Arrange
        var child = new User { Name = "Junior", Role = UserRole.Child, Balance = 10m, ConcurrencyStamp = 0 };
        Db.Users.Add(child);
        await Db.SaveChangesAsync();

        var scheduledTime = Now.AddMinutes(-5);
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

        // Act
        await _service.ProcessDueAllowancesAsync();

        // Assert
        var updated = await Db.Users.FindAsync(child.Id);
        Assert.Equal(1, updated!.ConcurrencyStamp);
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

    #region Timezone Fallback Tests

    [Fact]
    public async Task ProcessDueAllowancesAsync_InvalidTimeZone_FallsBackToUtcAndProcesses()
    {
        var child = new User { Name = "Junior", Role = UserRole.Child, Balance = 10m };
        Db.Users.Add(child);
        await Db.SaveChangesAsync();

        var scheduledTime = Now.AddMinutes(-5);
        var allowance = new ScheduledAllowance
        {
            ChildId = child.Id,
            Amount = 5m,
            DayOfWeek = DayOfWeek.Wednesday,
            TimeOfDay = new TimeOnly(9, 55),
            Description = "Weekly Allowance",
            IsActive = true,
            NextRunDate = scheduledTime,
            TimeZoneId = "Invalid/Timezone"
        };
        Db.ScheduledAllowances.Add(allowance);
        await Db.SaveChangesAsync();

        var count = await _service.ProcessDueAllowancesAsync();

        Assert.Equal(1, count);
        Assert.Equal(15m, child.Balance);
    }

    #endregion
}

