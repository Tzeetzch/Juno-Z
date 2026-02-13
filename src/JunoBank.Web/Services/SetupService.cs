using System.Text.Json;
using JunoBank.Web.Data;
using JunoBank.Web.Data.Entities;
using JunoBank.Web.Utils;
using Microsoft.EntityFrameworkCore;

namespace JunoBank.Web.Services;

/// <summary>
/// Service for managing first-run setup and user creation.
/// </summary>
public interface ISetupService
{
    /// <summary>
    /// Check if the setup wizard needs to be run (no admin exists).
    /// </summary>
    Task<bool> IsSetupRequiredAsync();

    /// <summary>
    /// Check if any admin exists in the system.
    /// </summary>
    Task<bool> HasAdminAsync();

    /// <summary>
    /// Create all users from the setup wizard in a single transaction.
    /// </summary>
    Task<SetupResult> CompleteSetupAsync(SetupData data);
}

/// <summary>
/// Data collected during the setup wizard.
/// </summary>
public class SetupData
{
    public required AdminData Admin { get; set; }
    public PartnerData? Partner { get; set; }
    public List<ChildData> Children { get; set; } = new();
    public EmailConfigData? Email { get; set; }
}

public class EmailConfigData
{
    public required string Host { get; set; }
    public int Port { get; set; } = 587;
    public required string Username { get; set; }
    public required string Password { get; set; }
    public string? FromEmail { get; set; }
}

public class AdminData
{
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
}

public class PartnerData
{
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
}

public class ChildData
{
    public required string Name { get; set; }
    public required DateTime Birthday { get; set; }
    public required decimal StartingBalance { get; set; }
    public required string[] PictureSequence { get; set; }
}

public class SetupResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public int? AdminUserId { get; init; }

    public static SetupResult Succeeded(int adminUserId) => new() { Success = true, AdminUserId = adminUserId };
    public static SetupResult Failed(string error) => new() { Success = false, Error = error };
}

public class SetupService : ISetupService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _configuration;

    public SetupService(AppDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    public async Task<bool> IsSetupRequiredAsync()
    {
        return !await HasAdminAsync();
    }

    public async Task<bool> HasAdminAsync()
    {
        return await _db.Users.AnyAsync(u => u.IsAdmin);
    }

    public async Task<SetupResult> CompleteSetupAsync(SetupData data)
    {
        // Validate email uniqueness
        var emails = new List<string> { data.Admin.Email.ToLowerInvariant() };
        if (data.Partner != null)
        {
            var partnerEmail = data.Partner.Email.ToLowerInvariant();
            if (emails.Contains(partnerEmail))
                return SetupResult.Failed("Partner email must be different from admin email.");
            emails.Add(partnerEmail);
        }

        var existingEmails = await _db.Users
            .Where(u => emails.Contains(u.Email!.ToLower()))
            .Select(u => u.Email)
            .ToListAsync();

        if (existingEmails.Any())
            return SetupResult.Failed($"Email already in use: {existingEmails.First()}");

        await using var transaction = await _db.Database.BeginTransactionAsync();

        try
        {
            // Create admin
            var admin = new User
            {
                Name = data.Admin.Name.Trim(),
                Role = UserRole.Parent,
                IsAdmin = true,
                Email = data.Admin.Email.ToLowerInvariant().Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(data.Admin.Password),
                Balance = 0,
                CreatedAt = DateTime.UtcNow
            };
            _db.Users.Add(admin);

            // Create partner if provided
            if (data.Partner != null)
            {
                var partner = new User
                {
                    Name = data.Partner.Name.Trim(),
                    Role = UserRole.Parent,
                    IsAdmin = false,
                    Email = data.Partner.Email.ToLowerInvariant().Trim(),
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(data.Partner.Password),
                    Balance = 0,
                    CreatedAt = DateTime.UtcNow
                };
                _db.Users.Add(partner);
            }

            await _db.SaveChangesAsync();

            // Create children
            foreach (var childData in data.Children)
            {
                var child = new User
                {
                    Name = childData.Name.Trim(),
                    Role = UserRole.Child,
                    IsAdmin = false,
                    Birthday = childData.Birthday,
                    Balance = childData.StartingBalance,
                    CreatedAt = DateTime.UtcNow
                };
                _db.Users.Add(child);
                await _db.SaveChangesAsync();

                // Add picture password
                var picturePassword = new PicturePassword
                {
                    UserId = child.Id,
                    ImageSequenceHash = SecurityUtils.HashPictureSequence(string.Join(",", childData.PictureSequence)),
                    GridSize = 9,
                    SequenceLength = 4
                };
                _db.PicturePasswords.Add(picturePassword);

                // Add initial transaction for starting balance if > 0
                if (childData.StartingBalance > 0)
                {
                    var initialDeposit = new Transaction
                    {
                        UserId = child.Id,
                        Amount = childData.StartingBalance,
                        Type = TransactionType.Deposit,
                        Description = "Welcome to Juno Bank!",
                        IsApproved = true,
                        ApprovedByUserId = admin.Id,
                        CreatedAt = DateTime.UtcNow
                    };
                    _db.Transactions.Add(initialDeposit);
                }
            }

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            // Write email config file if email settings were provided
            if (data.Email != null)
            {
                WriteEmailConfig(data.Email);
            }

            return SetupResult.Succeeded(admin.Id);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return SetupResult.Failed($"Setup failed: {ex.Message}");
        }
    }

    private void WriteEmailConfig(EmailConfigData email)
    {
        // Determine the data directory — use the DB connection string path, or fallback to "Data"
        var connStr = _configuration.GetConnectionString("DefaultConnection") ?? "Data Source=Data/junobank.db";
        var dbPath = connStr.Replace("Data Source=", "");
        var dataDir = Path.GetDirectoryName(dbPath) ?? "Data";

        Directory.CreateDirectory(dataDir);
        var configPath = Path.Combine(dataDir, "email-config.json");

        var configObj = new
        {
            Email = new
            {
                Host = email.Host,
                Port = email.Port,
                Username = email.Username,
                Password = email.Password,
                FromEmail = email.FromEmail ?? email.Username,
                UseSsl = true
            }
        };

        var json = JsonSerializer.Serialize(configObj, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(configPath, json);
    }
}
