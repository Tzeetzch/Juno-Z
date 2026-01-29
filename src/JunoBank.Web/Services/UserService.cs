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
                    .ToList()
            })
            .FirstOrDefaultAsync();

        return user ?? new ChildDashboardData();
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
