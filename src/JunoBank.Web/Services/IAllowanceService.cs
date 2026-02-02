using JunoBank.Web.Data.Entities;

namespace JunoBank.Web.Services;

/// <summary>
/// Service for managing scheduled allowances.
/// </summary>
public interface IAllowanceService
{
    /// <summary>
    /// Check if any allowances are due and process them.
    /// Handles catch-up for missed allowances (e.g., server was offline).
    /// </summary>
    Task<int> ProcessDueAllowancesAsync();

    /// <summary>
    /// Get the next scheduled run date for the active allowance.
    /// </summary>
    Task<DateTime?> GetNextRunDateAsync();

    /// <summary>
    /// Calculate the next run date based on interval type and time settings.
    /// </summary>
    DateTime CalculateNextRunDate(ScheduledAllowance allowance, DateTime fromDate);

    /// <summary>
    /// Calculate the next run date based on interval settings (for preview in UI).
    /// </summary>
    DateTime CalculateNextRunDate(
        AllowanceInterval interval,
        DayOfWeek dayOfWeek,
        int dayOfMonth,
        int monthOfYear,
        TimeOnly timeOfDay,
        DateTime fromDate);

    /// <summary>
    /// Get current allowance settings.
    /// </summary>
    Task<ScheduledAllowance?> GetAllowanceAsync();
}
