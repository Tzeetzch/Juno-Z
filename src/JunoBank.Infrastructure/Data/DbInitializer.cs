using JunoBank.Application.Utils;

namespace JunoBank.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext context, IPasswordService passwordService)
    {
        // Only seed demo data if JUNO_SEED_DEMO is set
        var seedDemo = Environment.GetEnvironmentVariable("JUNO_SEED_DEMO");
        if (!string.Equals(seedDemo, "true", StringComparison.OrdinalIgnoreCase))
            return;

        // Skip if already seeded
        if (context.Users.Any())
            return;

        // Create parents (first parent is admin)
        var parent1 = new User
        {
            Name = "Dad",
            Role = UserRole.Parent,
            IsAdmin = true,
            Email = "dad@junobank.local",
            PasswordHash = passwordService.HashPassword("parent123"),
            Balance = 0
        };

        var parent2 = new User
        {
            Name = "Mom",
            Role = UserRole.Parent,
            IsAdmin = false,
            Email = "mom@junobank.local",
            PasswordHash = passwordService.HashPassword("parent123"),
            Balance = 0
        };

        // Create children
        var child1 = new User
        {
            Name = "Junior",
            Role = UserRole.Child,
            Birthday = new DateTime(2020, 6, 15),
            Balance = 10.00m
        };

        var child2 = new User
        {
            Name = "Sophie",
            Role = UserRole.Child,
            Birthday = new DateTime(2022, 3, 22),
            Balance = 5.00m
        };

        context.Users.AddRange(parent1, parent2, child1, child2);
        await context.SaveChangesAsync();

        // Add picture password for Junior (sequence: cat, dog, star, moon)
        var picturePasswordJunior = new PicturePassword
        {
            UserId = child1.Id,
            ImageSequenceHash = SecurityUtils.HashPictureSequence("cat,dog,star,moon"),
            GridSize = 9,
            SequenceLength = 4
        };

        // Add picture password for Sophie (sequence: star, moon, cat, dog)
        var picturePasswordSophie = new PicturePassword
        {
            UserId = child2.Id,
            ImageSequenceHash = SecurityUtils.HashPictureSequence("star,moon,cat,dog"),
            GridSize = 9,
            SequenceLength = 4
        };

        context.PicturePasswords.AddRange(picturePasswordJunior, picturePasswordSophie);

        // Add initial transaction for Junior (starting balance)
        var initialDepositJunior = new Transaction
        {
            UserId = child1.Id,
            Amount = 10.00m,
            Type = TransactionType.Deposit,
            Description = "Welcome to Juno Bank!",
            IsApproved = true,
            ApprovedByUserId = parent1.Id
        };

        // Add initial transaction for Sophie (starting balance)
        var initialDepositSophie = new Transaction
        {
            UserId = child2.Id,
            Amount = 5.00m,
            Type = TransactionType.Deposit,
            Description = "Welcome to Juno Bank!",
            IsApproved = true,
            ApprovedByUserId = parent1.Id
        };

        context.Transactions.AddRange(initialDepositJunior, initialDepositSophie);

        // Add a pending request for Sophie (for E2E testing)
        var pendingRequestSophie = new MoneyRequest
        {
            ChildId = child2.Id,
            Amount = 2.00m,
            Type = RequestType.Deposit,
            Description = "For stickers",
            Status = RequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        context.MoneyRequests.Add(pendingRequestSophie);

        await context.SaveChangesAsync();
    }
}
