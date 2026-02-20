using JunoBank.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JunoBank.Application.Services;

/// <summary>
/// Service for managing scheduled allowances.
/// </summary>
public class AllowanceService : IAllowanceService
{
    private readonly IAppDbContext _db;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AllowanceService> _logger;

    public AllowanceService(IAppDbContext db, TimeProvider timeProvider, ILogger<AllowanceService> logger)
    {
        _db = db;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<int> ProcessDueAllowancesAsync()
    {
        var utcNow = _timeProvider.GetUtcNow().DateTime;
        var processedCount = 0;

        var dueAllowances = await _db.ScheduledAllowances
            .Include(a => a.Child)
            .Where(a => a.IsActive && a.NextRunDate <= utcNow)
            .ToListAsync();

        foreach (var allowance in dueAllowances)
        {
            var tz = GetTimeZone(allowance.TimeZoneId);

            while (allowance.NextRunDate <= utcNow)
            {
                _logger.LogInformation(
                    "Processing allowance for {ChildName}: {Amount:C} (scheduled for {ScheduledDate})",
                    allowance.Child.Name, allowance.Amount, allowance.NextRunDate);

                var transaction = new Transaction
                {
                    UserId = allowance.ChildId,
                    Amount = allowance.Amount,
                    Type = TransactionType.Allowance,
                    Description = allowance.Description,
                    IsApproved = true,
                    ApprovedByUserId = null,
                    CreatedAt = allowance.NextRunDate
                };

                _db.Transactions.Add(transaction);

                allowance.Child.Balance += allowance.Amount;
                allowance.Child.ConcurrencyStamp++;

                allowance.LastRunDate = allowance.NextRunDate;
                var localAfterRun = TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.SpecifyKind(allowance.NextRunDate.AddMinutes(1), DateTimeKind.Utc), tz);
                var nextLocal = CalculateNextRunDate(allowance, localAfterRun);
                allowance.NextRunDate = TimeZoneInfo.ConvertTimeToUtc(
                    DateTime.SpecifyKind(nextLocal, DateTimeKind.Unspecified), tz);

                processedCount++;
            }
        }

        if (processedCount > 0)
        {
            await _db.SaveChangesAsync();
            _logger.LogInformation("Processed {Count} allowance(s)", processedCount);
        }

        return processedCount;
    }

    public async Task<DateTime?> GetNextRunDateAsync()
    {
        var allowance = await _db.ScheduledAllowances
            .Where(a => a.IsActive)
            .Select(a => (DateTime?)a.NextRunDate)
            .FirstOrDefaultAsync();

        return allowance;
    }

    public DateTime CalculateNextRunDate(ScheduledAllowance allowance, DateTime fromDate)
    {
        return CalculateNextRunDate(
            allowance.Interval,
            allowance.DayOfWeek,
            allowance.DayOfMonth,
            allowance.MonthOfYear,
            allowance.TimeOfDay,
            fromDate);
    }

    public DateTime CalculateNextRunDate(
        AllowanceInterval interval,
        DayOfWeek dayOfWeek,
        int dayOfMonth,
        int monthOfYear,
        TimeOnly timeOfDay,
        DateTime fromDate)
    {
        return interval switch
        {
            AllowanceInterval.Hourly => CalculateNextHourly(fromDate),
            AllowanceInterval.Daily => CalculateNextDaily(timeOfDay, fromDate),
            AllowanceInterval.Weekly => CalculateNextWeekly(dayOfWeek, timeOfDay, fromDate),
            AllowanceInterval.Monthly => CalculateNextMonthly(dayOfMonth, timeOfDay, fromDate),
            AllowanceInterval.Yearly => CalculateNextYearly(dayOfMonth, monthOfYear, timeOfDay, fromDate),
            _ => throw new ArgumentOutOfRangeException(nameof(interval))
        };
    }

    private static DateTime CalculateNextHourly(DateTime fromDate)
    {
        var next = fromDate.AddHours(1);
        return new DateTime(next.Year, next.Month, next.Day, next.Hour, 0, 0);
    }

    private static DateTime CalculateNextDaily(TimeOnly timeOfDay, DateTime fromDate)
    {
        var todayAtTime = fromDate.Date.Add(timeOfDay.ToTimeSpan());
        if (fromDate < todayAtTime)
            return todayAtTime;
        return todayAtTime.AddDays(1);
    }

    private static DateTime CalculateNextWeekly(DayOfWeek dayOfWeek, TimeOnly timeOfDay, DateTime fromDate)
    {
        var daysUntilTarget = ((int)dayOfWeek - (int)fromDate.DayOfWeek + 7) % 7;

        if (daysUntilTarget == 0)
        {
            var todayAtTime = fromDate.Date.Add(timeOfDay.ToTimeSpan());
            if (fromDate < todayAtTime)
                return todayAtTime;
            daysUntilTarget = 7;
        }

        var nextDate = fromDate.Date.AddDays(daysUntilTarget);
        return nextDate.Add(timeOfDay.ToTimeSpan());
    }

    private static DateTime CalculateNextMonthly(int dayOfMonth, TimeOnly timeOfDay, DateTime fromDate)
    {
        dayOfMonth = Math.Clamp(dayOfMonth, 1, 28);
        var thisMonthAtDay = new DateTime(fromDate.Year, fromDate.Month, dayOfMonth).Add(timeOfDay.ToTimeSpan());
        if (fromDate < thisMonthAtDay)
            return thisMonthAtDay;
        var nextMonth = fromDate.AddMonths(1);
        return new DateTime(nextMonth.Year, nextMonth.Month, dayOfMonth).Add(timeOfDay.ToTimeSpan());
    }

    private static DateTime CalculateNextYearly(int dayOfMonth, int monthOfYear, TimeOnly timeOfDay, DateTime fromDate)
    {
        dayOfMonth = Math.Clamp(dayOfMonth, 1, 28);
        monthOfYear = Math.Clamp(monthOfYear, 1, 12);
        var thisYearAtDate = new DateTime(fromDate.Year, monthOfYear, dayOfMonth).Add(timeOfDay.ToTimeSpan());
        if (fromDate < thisYearAtDate)
            return thisYearAtDate;
        return new DateTime(fromDate.Year + 1, monthOfYear, dayOfMonth).Add(timeOfDay.ToTimeSpan());
    }

    public async Task<List<ScheduledAllowance>> GetOrdersForChildAsync(int childId)
    {
        return await _db.ScheduledAllowances
            .Where(a => a.ChildId == childId)
            .OrderBy(a => a.Description)
            .ToListAsync();
    }

    public async Task<ScheduledAllowance?> GetOrderByIdAsync(int orderId)
    {
        return await _db.ScheduledAllowances.FindAsync(orderId);
    }

    public async Task<ScheduledAllowance> CreateOrderAsync(ScheduledAllowance order)
    {
        var tz = GetTimeZone(order.TimeZoneId);
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(_timeProvider.GetUtcNow().DateTime, tz);
        var nextLocal = CalculateNextRunDate(order, localNow);
        order.NextRunDate = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(nextLocal, DateTimeKind.Unspecified), tz);
        _db.ScheduledAllowances.Add(order);
        await _db.SaveChangesAsync();
        return order;
    }

    public async Task UpdateOrderAsync(ScheduledAllowance order)
    {
        var tz = GetTimeZone(order.TimeZoneId);
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(_timeProvider.GetUtcNow().DateTime, tz);
        var nextLocal = CalculateNextRunDate(order, localNow);
        order.NextRunDate = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(nextLocal, DateTimeKind.Unspecified), tz);
        _db.ScheduledAllowances.Update(order);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteOrderAsync(int orderId)
    {
        var order = await _db.ScheduledAllowances.FindAsync(orderId);
        if (order != null)
        {
            _db.ScheduledAllowances.Remove(order);
            await _db.SaveChangesAsync();
        }
    }

    private TimeZoneInfo GetTimeZone(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Invalid timezone '{TimeZoneId}', falling back to UTC", timeZoneId);
            return TimeZoneInfo.Utc;
        }
    }
}
