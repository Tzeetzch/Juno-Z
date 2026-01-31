using JunoBank.Web.Data;
using JunoBank.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace JunoBank.Web.Services;

/// <summary>
/// Service for managing scheduled allowances.
/// Uses IDateTimeProvider for testability.
/// </summary>
public class AllowanceService : IAllowanceService
{
    private readonly AppDbContext _db;
    private readonly IDateTimeProvider _dateTime;
    private readonly ILogger<AllowanceService> _logger;

    public AllowanceService(AppDbContext db, IDateTimeProvider dateTime, ILogger<AllowanceService> logger)
    {
        _db = db;
        _dateTime = dateTime;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<int> ProcessDueAllowancesAsync()
    {
        var now = _dateTime.Now;
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
                allowance.NextRunDate = CalculateNextRunDate(
                    allowance.DayOfWeek,
                    allowance.TimeOfDay,
                    allowance.NextRunDate.AddMinutes(1) // Start from just after current run
                );

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
    public DateTime CalculateNextRunDate(DayOfWeek dayOfWeek, TimeOnly timeOfDay, DateTime fromDate)
    {
        // Find the next occurrence of the specified day of week
        var daysUntilTarget = ((int)dayOfWeek - (int)fromDate.DayOfWeek + 7) % 7;
        
        // If today is the target day, check if the time has passed
        if (daysUntilTarget == 0)
        {
            var todayAtTime = fromDate.Date.Add(timeOfDay.ToTimeSpan());
            if (fromDate < todayAtTime)
            {
                // Time hasn't passed yet today
                return todayAtTime;
            }
            // Time already passed, schedule for next week
            daysUntilTarget = 7;
        }

        var nextDate = fromDate.Date.AddDays(daysUntilTarget);
        return nextDate.Add(timeOfDay.ToTimeSpan());
    }

    /// <inheritdoc />
    public async Task UpdateAllowanceAsync(
        int parentUserId,
        decimal amount,
        DayOfWeek dayOfWeek,
        TimeOnly timeOfDay,
        string description,
        bool isActive)
    {
        var allowance = await _db.ScheduledAllowances.FirstOrDefaultAsync();

        if (allowance == null)
        {
            // Get the child (assumes single child for now)
            var child = await _db.Users.FirstAsync(u => u.Role == UserRole.Child);

            allowance = new ScheduledAllowance
            {
                ChildId = child.Id,
                CreatedByUserId = parentUserId,
                Amount = amount,
                DayOfWeek = dayOfWeek,
                TimeOfDay = timeOfDay,
                Description = description,
                IsActive = isActive,
                NextRunDate = isActive 
                    ? CalculateNextRunDate(dayOfWeek, timeOfDay, _dateTime.Now)
                    : DateTime.MaxValue,
                CreatedAt = _dateTime.UtcNow
            };

            _db.ScheduledAllowances.Add(allowance);
        }
        else
        {
            allowance.Amount = amount;
            allowance.DayOfWeek = dayOfWeek;
            allowance.TimeOfDay = timeOfDay;
            allowance.Description = description;
            allowance.IsActive = isActive;

            // Recalculate next run date if active
            if (isActive)
            {
                allowance.NextRunDate = CalculateNextRunDate(dayOfWeek, timeOfDay, _dateTime.Now);
            }
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Allowance updated: {Amount:C} on {Day}s at {Time}, Active: {IsActive}, Next: {NextRun}",
            amount, dayOfWeek, timeOfDay, isActive, allowance.NextRunDate);
    }

    /// <inheritdoc />
    public async Task<ScheduledAllowance?> GetAllowanceAsync()
    {
        return await _db.ScheduledAllowances.FirstOrDefaultAsync();
    }
}
