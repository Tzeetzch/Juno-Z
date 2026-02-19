using JunoBank.Application.DTOs;

namespace JunoBank.Application.Interfaces;

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
    Task<List<MoneyRequest>> GetPendingRequestsForChildAsync(int childId);
    Task<List<MoneyRequest>> GetCompletedRequestsForChildAsync(int childId, int skip = 0, int limit = 20);
    Task<Transaction> CreateManualTransactionForChildAsync(int parentUserId, int childId, decimal amount, TransactionType type, string description);
    Task<List<Transaction>> GetTransactionsForChildAsync(int childId, int skip = 0, int limit = 20);

    // User Management (Admin only)
    Task<List<ParentSummary>> GetAllParentsAsync();
    Task<User> CreateParentAsync(string name, string email, string password, bool isAdmin = false, int? callerUserId = null);
    Task<User> CreateChildAsync(string name, DateTime birthday, decimal startingBalance, string[] pictureSequence, int createdByUserId, bool requireAdmin = true);
    Task SetAdminStatusAsync(int userId, bool isAdmin, int callerUserId);
    Task<bool> IsAdminAsync(int userId);
    Task ResetParentPasswordAsync(int targetUserId, string newPassword, int callerAdminId);
    Task UpdatePicturePasswordAsync(int childId, string[] newSequence);
    Task UnlockChildAsync(int childId);
    Task<ChildLockoutStatus> GetChildLockoutStatusAsync(int childId);
}
