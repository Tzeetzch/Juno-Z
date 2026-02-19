namespace JunoBank.Application.Interfaces;

/// <summary>
/// Service for managing scheduled standing orders (recurring transfers).
/// </summary>
public interface IAllowanceService
{
    Task<int> ProcessDueAllowancesAsync();
    Task<List<ScheduledAllowance>> GetOrdersForChildAsync(int childId);
    Task<ScheduledAllowance?> GetOrderByIdAsync(int orderId);
    Task<ScheduledAllowance> CreateOrderAsync(ScheduledAllowance order);
    Task UpdateOrderAsync(ScheduledAllowance order);
    Task DeleteOrderAsync(int orderId);
    DateTime CalculateNextRunDate(ScheduledAllowance allowance, DateTime fromDate);
    DateTime CalculateNextRunDate(
        AllowanceInterval interval,
        DayOfWeek dayOfWeek,
        int dayOfMonth,
        int monthOfYear,
        TimeOnly timeOfDay,
        DateTime fromDate);
}
