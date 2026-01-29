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
}
