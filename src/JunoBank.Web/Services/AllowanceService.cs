using JunoBank.Web.Data;
using JunoBank.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace JunoBank.Web.Services;

/// <summary>
/// Service for managing scheduled allowances.
/// Uses TimeProvider for testability.
/// </summary>
public class AllowanceService : IAllowanceService
{
    private readonly AppDbContext _db;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AllowanceService> _logger;

    public AllowanceService(AppDbContext db, TimeProvider timeProvider, ILogger<AllowanceService> logger)
    {
        _db = db;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<int> ProcessDueAllowancesAsync()
    {
        var now = _timeProvider.GetLocalNow().DateTime;
        var processedCount = 0;

        // Get active allowances that are due (NextRunDate <= now)
        var dueAllowances = await _db.ScheduledAllowances
            .Include(a => a.Child)
            .Where(a => a.IsActive && a.NextRunDate <= now)
            .ToListAsync();

        foreach (var allowance in dueAllowances)
        {
            // Process all missed allowances (catch-up logic)
            while (allowance.NextRunDate <= now)
            {
                _logger.LogInformation(
                    "Processing allowance for {ChildName}: {Amount:C} (scheduled for {ScheduledDate})",
                    allowance.Child.Name, allowance.Amount, allowance.NextRunDate);

                // Create the transaction
                var transaction = new Transaction
                {
                    UserId = allowance.ChildId,
                    Amount = allowance.Amount,
                    Type = TransactionType.Allowance,
                    Description = allowance.Description,
                    IsApproved = true,
                    ApprovedByUserId = null, // System-generated, no human approver
                    CreatedAt = allowance.NextRunDate // Use scheduled time, not current time
                };

                _db.Transactions.Add(transaction);

                // Update child's balance
                allowance.Child.Balance += allowance.Amount;

                // Update schedule tracking
                allowance.LastRunDate = allowance.NextRunDate;
                allowance.NextRunDate = CalculateNextRunDate(allowance, allowance.NextRunDate.AddMinutes(1));

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

    /// <inheritdoc />
    public async Task<DateTime?> GetNextRunDateAsync()
    {
        var allowance = await _db.ScheduledAllowances
            .Where(a => a.IsActive)
            .Select(a => (DateTime?)a.NextRunDate)
            .FirstOrDefaultAsync();

        return allowance;
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
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
        // Next hour at minute 0
        var next = fromDate.AddHours(1);
        return new DateTime(next.Year, next.Month, next.Day, next.Hour, 0, 0);
    }

    private static DateTime CalculateNextDaily(TimeOnly timeOfDay, DateTime fromDate)
    {
        var todayAtTime = fromDate.Date.Add(timeOfDay.ToTimeSpan());
        
        if (fromDate < todayAtTime)
        {
            return todayAtTime;
        }
        
        return todayAtTime.AddDays(1);
    }

    private static DateTime CalculateNextWeekly(DayOfWeek dayOfWeek, TimeOnly timeOfDay, DateTime fromDate)
    {
        var daysUntilTarget = ((int)dayOfWeek - (int)fromDate.DayOfWeek + 7) % 7;
        
        if (daysUntilTarget == 0)
        {
            var todayAtTime = fromDate.Date.Add(timeOfDay.ToTimeSpan());
            if (fromDate < todayAtTime)
            {
                return todayAtTime;
            }
            daysUntilTarget = 7;
        }

        var nextDate = fromDate.Date.AddDays(daysUntilTarget);
        return nextDate.Add(timeOfDay.ToTimeSpan());
    }

    private static DateTime CalculateNextMonthly(int dayOfMonth, TimeOnly timeOfDay, DateTime fromDate)
    {
        // Clamp day to valid range (1-28 to be safe for all months)
        dayOfMonth = Math.Clamp(dayOfMonth, 1, 28);
        
        var thisMonthAtDay = new DateTime(fromDate.Year, fromDate.Month, dayOfMonth).Add(timeOfDay.ToTimeSpan());
        
        if (fromDate < thisMonthAtDay)
        {
            return thisMonthAtDay;
        }
        
        // Move to next month
        var nextMonth = fromDate.AddMonths(1);
        return new DateTime(nextMonth.Year, nextMonth.Month, dayOfMonth).Add(timeOfDay.ToTimeSpan());
    }

    private static DateTime CalculateNextYearly(int dayOfMonth, int monthOfYear, TimeOnly timeOfDay, DateTime fromDate)
    {
        // Clamp values
        dayOfMonth = Math.Clamp(dayOfMonth, 1, 28);
        monthOfYear = Math.Clamp(monthOfYear, 1, 12);
        
        var thisYearAtDate = new DateTime(fromDate.Year, monthOfYear, dayOfMonth).Add(timeOfDay.ToTimeSpan());
        
        if (fromDate < thisYearAtDate)
        {
            return thisYearAtDate;
        }
        
        // Move to next year
        return new DateTime(fromDate.Year + 1, monthOfYear, dayOfMonth).Add(timeOfDay.ToTimeSpan());
    }

    /// <inheritdoc />
    public async Task<List<ScheduledAllowance>> GetOrdersForChildAsync(int childId)
    {
        return await _db.ScheduledAllowances
            .Where(a => a.ChildId == childId)
            .OrderBy(a => a.Description)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<ScheduledAllowance?> GetOrderByIdAsync(int orderId)
    {
        return await _db.ScheduledAllowances.FindAsync(orderId);
    }

    /// <inheritdoc />
    public async Task<ScheduledAllowance> CreateOrderAsync(ScheduledAllowance order)
    {
        order.NextRunDate = CalculateNextRunDate(order, _timeProvider.GetLocalNow().DateTime);
        _db.ScheduledAllowances.Add(order);
        await _db.SaveChangesAsync();
        return order;
    }

    /// <inheritdoc />
    public async Task UpdateOrderAsync(ScheduledAllowance order)
    {
        order.NextRunDate = CalculateNextRunDate(order, _timeProvider.GetLocalNow().DateTime);
        _db.ScheduledAllowances.Update(order);
        await _db.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteOrderAsync(int orderId)
    {
        var order = await _db.ScheduledAllowances.FindAsync(orderId);
        if (order != null)
        {
            _db.ScheduledAllowances.Remove(order);
            await _db.SaveChangesAsync();
        }
    }
}
