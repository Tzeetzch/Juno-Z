using System.Security.Cryptography;
using System.Text;
using JunoBank.Web.Data.Entities;

namespace JunoBank.Web.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext context)
    {
        // Skip if already seeded
        if (context.Users.Any())
            return;

        // Create parents
        var parent1 = new User
        {
            Name = "Dad",
            Role = UserRole.Parent,
            Email = "dad@junobank.local",
            PasswordHash = HashPassword("parent123"),
            Balance = 0
        };

        var parent2 = new User
        {
            Name = "Mom",
            Role = UserRole.Parent,
            Email = "mom@junobank.local",
            PasswordHash = HashPassword("parent123"),
            Balance = 0
        };

        // Create child
        var child = new User
        {
            Name = "Junior",
            Role = UserRole.Child,
            Balance = 10.00m // Starting balance of €10
        };

        context.Users.AddRange(parent1, parent2, child);
        await context.SaveChangesAsync();

        // Add picture password for child (sequence: cat, dog, star, moon)
        var picturePassword = new PicturePassword
        {
            UserId = child.Id,
            ImageSequenceHash = HashSequence("cat,dog,star,moon"),
            GridSize = 9,
            SequenceLength = 4
        };

        context.PicturePasswords.Add(picturePassword);

        // Add initial transaction (starting balance)
        var initialDeposit = new Transaction
        {
            UserId = child.Id,
            Amount = 10.00m,
            Type = TransactionType.Deposit,
            Description = "Welcome to Juno Bank!",
            IsApproved = true,
            ApprovedByUserId = parent1.Id
        };

        context.Transactions.Add(initialDeposit);
        await context.SaveChangesAsync();
    }

    private static string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    private static string HashSequence(string sequence)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(sequence));
        return Convert.ToBase64String(bytes);
    }
}
