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
    
    // Multi-child support
    Task<List<ChildSummary>> GetAllChildrenSummaryAsync();
    Task<User?> GetChildByIdAsync(int childId);
    Task<int> GetOpenRequestCountAsync(int childId);
    
    /// <summary>
    /// Gets pending requests for a specific child.
    /// </summary>
    Task<List<MoneyRequest>> GetPendingRequestsForChildAsync(int childId);
    
    /// <summary>
    /// Gets completed (approved/denied) requests for a specific child.
    /// </summary>
    Task<List<MoneyRequest>> GetCompletedRequestsForChildAsync(int childId, int skip = 0, int limit = 20);
    
    /// <summary>
    /// Creates a manual transaction for a specific child.
    /// </summary>
    Task<Transaction> CreateManualTransactionForChildAsync(int parentUserId, int childId, decimal amount, TransactionType type, string description);
    
    /// <summary>
    /// Gets transactions for a specific child.
    /// </summary>
    Task<List<Transaction>> GetTransactionsForChildAsync(int childId, int skip = 0, int limit = 20);

    // User Management (Admin only)
    
    /// <summary>
    /// Gets all parents for user management.
    /// </summary>
    Task<List<ParentSummary>> GetAllParentsAsync();

    /// <summary>
    /// Creates a new parent user.
    /// </summary>
    Task<User> CreateParentAsync(string name, string email, string password, bool isAdmin = false);

    /// <summary>
    /// Creates a new child user.
    /// </summary>
    Task<User> CreateChildAsync(string name, DateTime birthday, decimal startingBalance, string[] pictureSequence, int createdByUserId);

    /// <summary>
    /// Updates a user's admin status.
    /// </summary>
    Task SetAdminStatusAsync(int userId, bool isAdmin);

    /// <summary>
    /// Checks if a user is an admin.
    /// </summary>
    Task<bool> IsAdminAsync(int userId);
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

/// <summary>
/// Summary DTO for displaying child cards on parent dashboard.
/// </summary>
public class ChildSummary
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public int PendingRequestCount { get; set; }
}

/// <summary>
/// Summary DTO for displaying parents in user management.
/// </summary>
public class ParentSummary
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
}
