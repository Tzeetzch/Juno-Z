using JunoBank.Web.Data.Entities;

namespace JunoBank.Web.Services;

public interface IUserService
{
    Task<decimal> GetBalanceAsync(int userId);
    Task<List<Transaction>> GetRecentTransactionsAsync(int userId, int limit = 50);
    Task<ChildDashboardData> GetChildDashboardDataAsync(int userId);
    Task<MoneyRequest> CreateMoneyRequestAsync(int childId, decimal amount, RequestType type, string description);
    Task<ParentDashboardData> GetParentDashboardDataAsync();
    Task<List<MoneyRequest>> GetPendingRequestsAsync();
    Task ResolveRequestAsync(int requestId, int parentUserId, bool approve, string? parentNote = null);
    Task<Transaction> CreateManualTransactionAsync(int parentUserId, decimal amount, TransactionType type, string description);
    Task<List<Transaction>> GetAllTransactionsAsync(int limit = 100);
}

public class ParentDashboardData
{
    public string ChildName { get; set; } = string.Empty;
    public decimal ChildBalance { get; set; }
    public int PendingRequestCount { get; set; }
}

public class ChildDashboardData
{
    public string Name { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public List<Transaction> RecentTransactions { get; set; } = new();
    public List<MoneyRequest> RecentRequests { get; set; } = new();
}
