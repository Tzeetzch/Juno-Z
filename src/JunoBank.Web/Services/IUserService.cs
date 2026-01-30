using JunoBank.Web.Data.Entities;

namespace JunoBank.Web.Services;

public interface IUserService
{
    Task<decimal> GetBalanceAsync(int userId);
    Task<List<Transaction>> GetRecentTransactionsAsync(int userId, int limit = 50);
    Task<ChildDashboardData> GetChildDashboardDataAsync(int userId);
    Task<MoneyRequest> CreateMoneyRequestAsync(int childId, decimal amount, RequestType type, string description);
}

public class ChildDashboardData
{
    public string Name { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public List<Transaction> RecentTransactions { get; set; } = new();
    public List<MoneyRequest> RecentRequests { get; set; } = new();
}
