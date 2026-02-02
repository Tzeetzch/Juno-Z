using JunoBank.Web.Data;
using JunoBank.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace JunoBank.Web.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _db;

    public UserService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<decimal> GetBalanceAsync(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        return user?.Balance ?? 0;
    }

    public async Task<List<Transaction>> GetRecentTransactionsAsync(int userId, int limit = 50)
    {
        return await _db.Transactions
            .Where(t => t.UserId == userId && t.IsApproved)
            .OrderByDescending(t => t.CreatedAt)
            .Take(limit)  // ✅ FIXED: Always limit results
            .ToListAsync();
    }

    public async Task<ChildDashboardData> GetChildDashboardDataAsync(int userId)
    {
        // ✅ FIXED: Single query instead of multiple round-trips
        var user = await _db.Users
            .Where(u => u.Id == userId)
            .Select(u => new ChildDashboardData
            {
                Name = u.Name,
                Balance = u.Balance,
                RecentTransactions = u.Transactions
                    .Where(t => t.IsApproved)
                    .OrderByDescending(t => t.CreatedAt)
                    .Take(50)
                    .ToList(),
                RecentRequests = u.MoneyRequests
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(20)
                    .ToList()
            })
            .FirstOrDefaultAsync();

        return user ?? new ChildDashboardData();
    }

    public async Task<ParentDashboardData> GetParentDashboardDataAsync()
    {
        var child = await _db.Users
            .Where(u => u.Role == UserRole.Child)
            .Select(u => new { u.Name, u.Balance })
            .FirstOrDefaultAsync();

        var pendingCount = await _db.MoneyRequests
            .CountAsync(r => r.Status == RequestStatus.Pending);

        return new ParentDashboardData
        {
            ChildName = child?.Name ?? "Child",
            ChildBalance = child?.Balance ?? 0,
            PendingRequestCount = pendingCount
        };
    }

    public async Task<List<MoneyRequest>> GetPendingRequestsAsync()
    {
        return await _db.MoneyRequests
            .Include(r => r.Child)
            .Where(r => r.Status == RequestStatus.Pending)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task ResolveRequestAsync(int requestId, int parentUserId, bool approve, string? parentNote = null)
    {
        var request = await _db.MoneyRequests
            .Include(r => r.Child)
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (request == null)
            throw new ArgumentException("Request not found");

        if (request.Status != RequestStatus.Pending)
            throw new InvalidOperationException("Request has already been resolved");

        var parent = await _db.Users.FindAsync(parentUserId);
        if (parent == null || parent.Role != UserRole.Parent)
            throw new InvalidOperationException("Only parents can resolve requests");

        request.Status = approve ? RequestStatus.Approved : RequestStatus.Denied;
        request.ResolvedByUserId = parentUserId;
        request.ParentNote = parentNote;
        request.ResolvedAt = DateTime.UtcNow;

        if (approve)
        {
            var child = request.Child;

            if (request.Type == RequestType.Withdrawal)
            {
                if (child.Balance < request.Amount)
                    throw new InvalidOperationException("Insufficient balance for withdrawal");

                child.Balance -= request.Amount;
            }
            else
            {
                child.Balance += request.Amount;
            }

            // Create a matching transaction record
            var transaction = new Transaction
            {
                UserId = child.Id,
                Amount = request.Amount,
                Type = request.Type == RequestType.Deposit ? TransactionType.Deposit : TransactionType.Withdrawal,
                Description = request.Description,
                IsApproved = true,
                ApprovedByUserId = parentUserId,
                CreatedAt = DateTime.UtcNow
            };

            _db.Transactions.Add(transaction);
        }

        await _db.SaveChangesAsync();
    }

    public async Task<List<Transaction>> GetAllTransactionsAsync(int limit = 100)
    {
        return await _db.Transactions
            .OrderByDescending(t => t.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<Transaction> CreateManualTransactionAsync(int parentUserId, decimal amount, TransactionType type, string description)
    {
        var parent = await _db.Users.FindAsync(parentUserId);
        if (parent == null || parent.Role != UserRole.Parent)
            throw new InvalidOperationException("Only parents can create manual transactions");

        var child = await _db.Users.FirstOrDefaultAsync(u => u.Role == UserRole.Child);
        if (child == null)
            throw new InvalidOperationException("No child account found");

        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero", nameof(amount));

        if (amount > 1000)
            throw new ArgumentException("Amount cannot exceed €1000", nameof(amount));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required", nameof(description));

        if (type == TransactionType.Withdrawal && child.Balance < amount)
            throw new InvalidOperationException("Insufficient balance for withdrawal");

        if (type == TransactionType.Deposit)
            child.Balance += amount;
        else if (type == TransactionType.Withdrawal)
            child.Balance -= amount;

        var transaction = new Transaction
        {
            UserId = child.Id,
            Amount = amount,
            Type = type,
            Description = description,
            IsApproved = true,
            ApprovedByUserId = parentUserId,
            CreatedAt = DateTime.UtcNow
        };

        _db.Transactions.Add(transaction);
        await _db.SaveChangesAsync();

        return transaction;
    }

    public async Task<MoneyRequest> CreateMoneyRequestAsync(int childId, decimal amount, RequestType type, string description)
    {
        // Verify the user exists and is a child
        var child = await _db.Users.FindAsync(childId);

        if (child == null)
            throw new ArgumentException("User not found", nameof(childId));

        if (child.Role != UserRole.Child)
            throw new InvalidOperationException("Only children can create money requests");

        // Validate amount
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero", nameof(amount));

        if (amount > 1000)
            throw new ArgumentException("Amount cannot exceed €1000", nameof(amount));

        // Validate description
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required", nameof(description));

        if (description.Length > 500)
            throw new ArgumentException("Description is too long (max 500 characters)", nameof(description));

        var request = new MoneyRequest
        {
            ChildId = childId,
            Amount = amount,
            Type = type,
            Description = description,
            Status = RequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _db.MoneyRequests.Add(request);
        await _db.SaveChangesAsync();

        return request;
    }
}
