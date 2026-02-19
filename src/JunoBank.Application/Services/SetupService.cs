using JunoBank.Application.DTOs;
using JunoBank.Application.Interfaces;
using JunoBank.Application.Utils;
using Microsoft.EntityFrameworkCore;

namespace JunoBank.Application.Services;

public class SetupService : ISetupService
{
    private readonly IAppDbContext _db;
    private readonly IEmailConfigService _emailConfig;
    private readonly IPasswordService _passwordService;

    public SetupService(IAppDbContext db, IEmailConfigService emailConfig, IPasswordService passwordService)
    {
        _db = db;
        _emailConfig = emailConfig;
        _passwordService = passwordService;
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
            var admin = new User
            {
                Name = data.Admin.Name.Trim(),
                Role = UserRole.Parent,
                IsAdmin = true,
                Email = data.Admin.Email.ToLowerInvariant().Trim(),
                PasswordHash = _passwordService.HashPassword(data.Admin.Password),
                Balance = 0,
                CreatedAt = DateTime.UtcNow
            };
            _db.Users.Add(admin);

            if (data.Partner != null)
            {
                var partner = new User
                {
                    Name = data.Partner.Name.Trim(),
                    Role = UserRole.Parent,
                    IsAdmin = false,
                    Email = data.Partner.Email.ToLowerInvariant().Trim(),
                    PasswordHash = _passwordService.HashPassword(data.Partner.Password),
                    Balance = 0,
                    CreatedAt = DateTime.UtcNow
                };
                _db.Users.Add(partner);
            }

            await _db.SaveChangesAsync();

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

                var picturePassword = new PicturePassword
                {
                    UserId = child.Id,
                    ImageSequenceHash = SecurityUtils.HashPictureSequence(string.Join(",", childData.PictureSequence)),
                    GridSize = 9,
                    SequenceLength = 4
                };
                _db.PicturePasswords.Add(picturePassword);

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

            if (data.Email != null)
            {
                await _emailConfig.SaveEmailConfigAsync(data.Email);
            }

            return SetupResult.Succeeded(admin.Id);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return SetupResult.Failed($"Setup failed: {ex.Message}");
        }
    }
}
