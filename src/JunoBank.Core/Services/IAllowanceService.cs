using JunoBank.Web.Data.Entities;

namespace JunoBank.Web.Services;

/// <summary>
/// Service for managing scheduled standing orders (recurring transfers).
/// </summary>
public interface IAllowanceService
{
    /// <summary>
    /// Check if any orders are due and process them.
    /// Handles catch-up for missed orders (e.g., server was offline).
    /// </summary>
    Task<int> ProcessDueAllowancesAsync();

    /// <summary>
    /// Get all standing orders for a specific child.
    /// </summary>
    Task<List<ScheduledAllowance>> GetOrdersForChildAsync(int childId);

    /// <summary>
    /// Get a specific standing order by ID.
    /// </summary>
    Task<ScheduledAllowance?> GetOrderByIdAsync(int orderId);

    /// <summary>
    /// Create a new standing order.
    /// </summary>
    Task<ScheduledAllowance> CreateOrderAsync(ScheduledAllowance order);

    /// <summary>
    /// Update an existing standing order.
    /// </summary>
    Task UpdateOrderAsync(ScheduledAllowance order);

    /// <summary>
    /// Delete a standing order.
    /// </summary>
    Task DeleteOrderAsync(int orderId);

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
}
