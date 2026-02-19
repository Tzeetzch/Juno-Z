using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace JunoBank.Application.Interfaces;

/// <summary>
/// Abstraction over AppDbContext for dependency inversion.
/// Services depend on this interface, not the concrete EF Core DbContext.
/// </summary>
public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<PicturePassword> PicturePasswords { get; }
    DbSet<Transaction> Transactions { get; }
    DbSet<MoneyRequest> MoneyRequests { get; }
    DbSet<ScheduledAllowance> ScheduledAllowances { get; }
    DbSet<PasswordResetToken> PasswordResetTokens { get; }
    DbSet<NotificationPreference> NotificationPreferences { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    ChangeTracker ChangeTracker { get; }
    DatabaseFacade Database { get; }
}
