using JunoBank.Web.Data;
using JunoBank.Web.Data.Entities;
using JunoBank.Web.Utils;
using Microsoft.EntityFrameworkCore;

namespace JunoBank.Web.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _db;
    private readonly ILogger<UserService> _logger;

    public UserService(AppDbContext db, ILogger<UserService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Validates that the given userId belongs to a parent. Throws UnauthorizedAccessException if not.
    /// </summary>
    private async Task<User> RequireParentAsync(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null || user.Role != UserRole.Parent)
            throw new UnauthorizedAccessException("Only parents can perform this action.");
        return user;
    }

    /// <summary>
    /// Validates that the given userId belongs to an admin. Throws UnauthorizedAccessException if not.
    /// </summary>
    private async Task<User> RequireAdminAsync(int userId)
    {
        var user = await RequireParentAsync(userId);
        if (!user.IsAdmin)
            throw new UnauthorizedAccessException("Only administrators can perform this action.");
        return user;
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

    public async Task<List<MoneyRequest>> GetPendingRequestsForChildAsync(int childId)
    {
        return await _db.MoneyRequests
            .Include(r => r.Child)
            .Where(r => r.ChildId == childId && r.Status == RequestStatus.Pending)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<MoneyRequest>> GetCompletedRequestsForChildAsync(int childId, int skip = 0, int limit = 20)
    {
        return await _db.MoneyRequests
            .Include(r => r.Child)
            .Where(r => r.ChildId == childId && r.Status != RequestStatus.Pending)
            .OrderByDescending(r => r.ResolvedAt)
            .Skip(skip)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<Transaction>> GetTransactionsForChildAsync(int childId, int skip = 0, int limit = 20)
    {
        return await _db.Transactions
            .Where(t => t.UserId == childId)
            .OrderByDescending(t => t.CreatedAt)
            .Skip(skip)
            .Take(limit)
            .ToListAsync();
    }

    public async Task ResolveRequestAsync(int requestId, int parentUserId, bool approve, string? parentNote = null)
    {
        await RequireParentAsync(parentUserId);

        var request = await _db.MoneyRequests
            .Include(r => r.Child)
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (request == null)
            throw new ArgumentException("Request not found");

        if (request.Status != RequestStatus.Pending)
            throw new InvalidOperationException("Request has already been resolved");

        _logger.LogInformation("Parent {ParentId} {Action} request {RequestId} for child {ChildId} ({Amount:C})",
            parentUserId, approve ? "approving" : "denying", requestId, request.ChildId, request.Amount);

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
        await RequireParentAsync(parentUserId);
        _logger.LogInformation("Parent {ParentId} creating {Type} of {Amount:C}", parentUserId, type, amount);

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

    public async Task<Transaction> CreateManualTransactionForChildAsync(int parentUserId, int childId, decimal amount, TransactionType type, string description)
    {
        await RequireParentAsync(parentUserId);
        _logger.LogInformation("Parent {ParentId} creating {Type} of {Amount:C} for child {ChildId}", parentUserId, type, amount, childId);

        var child = await _db.Users.FirstOrDefaultAsync(u => u.Id == childId && u.Role == UserRole.Child);
        if (child == null)
            throw new InvalidOperationException("Child account not found");

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

        // Check open request limit (max 5 per child)
        var openRequestCount = await GetOpenRequestCountAsync(childId);
        if (openRequestCount >= 5)
            throw new InvalidOperationException("Maximum of 5 pending requests allowed. Please wait for a parent to review your existing requests.");

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

    public async Task<List<ChildSummary>> GetAllChildrenSummaryAsync()
    {
        return await _db.Users
            .Where(u => u.Role == UserRole.Child)
            .Select(u => new ChildSummary
            {
                Id = u.Id,
                Name = u.Name,
                Balance = u.Balance,
                PendingRequestCount = u.MoneyRequests.Count(r => r.Status == RequestStatus.Pending)
            })
            .ToListAsync();
    }

    public async Task<User?> GetChildByIdAsync(int childId)
    {
        return await _db.Users
            .FirstOrDefaultAsync(u => u.Id == childId && u.Role == UserRole.Child);
    }

    public async Task<int> GetOpenRequestCountAsync(int childId)
    {
        return await _db.MoneyRequests
            .CountAsync(r => r.ChildId == childId && r.Status == RequestStatus.Pending);
    }

    // ========== User Management Methods (Admin) ==========

    public async Task<List<ParentSummary>> GetAllParentsAsync()
    {
        return await _db.Users
            .Where(u => u.Role == UserRole.Parent)
            .Select(u => new ParentSummary
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email ?? "",
                IsAdmin = u.IsAdmin
            })
            .ToListAsync();
    }

    public async Task<User> CreateParentAsync(string name, string email, string password, bool isAdmin = false, int? callerUserId = null)
    {
        if (callerUserId.HasValue)
            await RequireAdminAsync(callerUserId.Value);
        _logger.LogInformation("Creating parent account for {Email} (by user {CallerId})", email, callerUserId);

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

        var parent = new User
        {
            Name = name,
            Email = email,
            PasswordHash = passwordHash,
            Role = UserRole.Parent,
            IsAdmin = isAdmin,
            Balance = 0,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(parent);
        await _db.SaveChangesAsync();

        return parent;
    }

    public async Task<User> CreateChildAsync(string name, DateTime birthday, decimal startingBalance, string[] pictureSequence, int createdByUserId, bool requireAdmin = true)
    {
        if (requireAdmin)
            await RequireAdminAsync(createdByUserId);
        _logger.LogInformation("Creating child account {Name} (by user {CallerId})", name, createdByUserId);

        var child = new User
        {
            Name = name,
            Birthday = birthday,
            Role = UserRole.Child,
            Balance = startingBalance,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(child);
        await _db.SaveChangesAsync();

        // Create picture password
        var picturePassword = new PicturePassword
        {
            UserId = child.Id,
            ImageSequenceHash = SecurityUtils.HashPictureSequence(string.Join(",", pictureSequence)),
            GridSize = 9,
            SequenceLength = 4
        };

        _db.PicturePasswords.Add(picturePassword);

        // Create opening balance transaction if balance > 0
        if (startingBalance > 0)
        {
            var transaction = new Transaction
            {
                UserId = child.Id,
                Amount = startingBalance,
                Type = TransactionType.Deposit,
                Description = "Opening balance",
                IsApproved = true,
                ApprovedByUserId = createdByUserId,
                CreatedAt = DateTime.UtcNow
            };
            _db.Transactions.Add(transaction);
        }

        await _db.SaveChangesAsync();

        return child;
    }

    public async Task SetAdminStatusAsync(int userId, bool isAdmin, int callerUserId)
    {
        await RequireAdminAsync(callerUserId);
        _logger.LogInformation("Admin {CallerId} setting admin status of user {UserId} to {IsAdmin}", callerUserId, userId, isAdmin);

        var user = await _db.Users.FindAsync(userId);
        if (user != null && user.Role == UserRole.Parent)
        {
            user.IsAdmin = isAdmin;
            await _db.SaveChangesAsync();
        }
    }

    public async Task<bool> IsAdminAsync(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        return user?.IsAdmin ?? false;
    }
}
