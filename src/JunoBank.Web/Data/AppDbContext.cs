using Microsoft.EntityFrameworkCore;

namespace JunoBank.Web.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // Entities will be added in Phase B
    // public DbSet<User> Users => Set<User>();
    // public DbSet<Transaction> Transactions => Set<Transaction>();
    // public DbSet<MoneyRequest> MoneyRequests => Set<MoneyRequest>();
    // public DbSet<ScheduledAllowance> ScheduledAllowances => Set<ScheduledAllowance>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Entity configurations will be added in Phase B
    }
}
