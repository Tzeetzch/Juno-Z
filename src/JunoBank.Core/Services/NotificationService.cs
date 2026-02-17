using JunoBank.Web.Data;
using JunoBank.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace JunoBank.Web.Services;

public interface INotificationService
{
    /// <summary>
    /// Get a parent's notification preference for a specific type.
    /// Returns null if no preference exists yet.
    /// </summary>
    Task<NotificationPreference?> GetPreferenceAsync(int userId, NotificationType type);

    /// <summary>
    /// Create or update a notification preference.
    /// </summary>
    Task<NotificationPreference> UpsertPreferenceAsync(NotificationPreference preference);

    /// <summary>
    /// Check all enabled notification preferences and send any that are due.
    /// Returns the number of notifications sent.
    /// </summary>
    Task<int> ProcessDueNotificationsAsync();
}

public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;
    private readonly TimeProvider _timeProvider;
    private readonly IEmailService _emailService;
    private readonly IEmailConfigService _emailConfigService;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        AppDbContext db,
        TimeProvider timeProvider,
        IEmailService emailService,
        IEmailConfigService emailConfigService,
        ILogger<NotificationService> logger)
    {
        _db = db;
        _timeProvider = timeProvider;
        _emailService = emailService;
        _emailConfigService = emailConfigService;
        _logger = logger;
    }

    public async Task<NotificationPreference?> GetPreferenceAsync(int userId, NotificationType type)
    {
        return await _db.NotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Type == type);
    }

    public async Task<NotificationPreference> UpsertPreferenceAsync(NotificationPreference preference)
    {
        var existing = await _db.NotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == preference.UserId && p.Type == preference.Type);

        if (existing != null)
        {
            existing.Enabled = preference.Enabled;
            existing.DayOfWeek = preference.DayOfWeek;
            existing.TimeOfDay = preference.TimeOfDay;
            existing.TimeZoneId = preference.TimeZoneId;
        }
        else
        {
            _db.NotificationPreferences.Add(preference);
            existing = preference;
        }

        await _db.SaveChangesAsync();
        return existing;
    }

    public async Task<int> ProcessDueNotificationsAsync()
    {
        if (!_emailConfigService.IsConfigured)
            return 0;

        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;

        var preferences = await _db.NotificationPreferences
            .Include(p => p.User)
            .Where(p => p.Enabled && p.Type == NotificationType.WeeklySummary)
            .ToListAsync();

        var sentCount = 0;

        foreach (var pref in preferences)
        {
            if (!IsDueToSend(pref, utcNow))
                continue;

            try
            {
                await SendWeeklySummaryAsync(pref, utcNow);
                pref.LastSentDate = utcNow;
                await _db.SaveChangesAsync();
                sentCount++;
                _logger.LogInformation("Weekly summary sent to {Email}", pref.User.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send weekly summary to user {UserId}", pref.UserId);
            }
        }

        return sentCount;
    }

    public static bool IsDueToSend(NotificationPreference pref, DateTime utcNow)
    {
        var tz = GetTimeZone(pref.TimeZoneId);
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(utcNow, DateTimeKind.Utc), tz);

        // Must be the right day of week
        if (localNow.DayOfWeek != pref.DayOfWeek)
            return false;

        // Must be within 10 minutes after the scheduled time
        var scheduledTime = localNow.Date.Add(pref.TimeOfDay.ToTimeSpan());
        var diff = localNow - scheduledTime;
        if (diff < TimeSpan.Zero || diff > TimeSpan.FromMinutes(10))
            return false;

        // Must not have been sent in the last 6 days (prevents double-send)
        if (pref.LastSentDate.HasValue)
        {
            var daysSinceLastSend = (utcNow - pref.LastSentDate.Value).TotalDays;
            if (daysSinceLastSend < 6)
                return false;
        }

        return true;
    }

    private async Task SendWeeklySummaryAsync(NotificationPreference pref, DateTime utcNow)
    {
        var email = pref.User.Email;
        if (string.IsNullOrWhiteSpace(email))
            return;

        var weekEnd = utcNow.Date;
        var weekStart = weekEnd.AddDays(-7);

        // Get all children
        var children = await _db.Users
            .Where(u => u.Role == UserRole.Child)
            .OrderBy(u => u.Name)
            .ToListAsync();

        var childSummaries = new List<ChildWeeklySummary>();

        foreach (var child in children)
        {
            // Transactions in the last 7 days
            var transactions = await _db.Transactions
                .Where(t => t.UserId == child.Id && t.CreatedAt >= weekStart && t.CreatedAt < weekEnd.AddDays(1))
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            var deposits = transactions.Where(t => t.Type != TransactionType.Withdrawal).Sum(t => t.Amount);
            var withdrawals = transactions.Where(t => t.Type == TransactionType.Withdrawal).Sum(t => t.Amount);

            // Pending requests
            var pendingCount = await _db.MoneyRequests
                .CountAsync(r => r.ChildId == child.Id && r.Status == RequestStatus.Pending);

            childSummaries.Add(new ChildWeeklySummary
            {
                Name = child.Name,
                Balance = child.Balance,
                BalanceChange = deposits - withdrawals,
                PendingRequests = pendingCount,
                Transactions = transactions.Select(t => new TransactionSummary
                {
                    CreatedAt = t.CreatedAt,
                    Description = t.Description,
                    Amount = t.Amount,
                    IsDeposit = t.Type != TransactionType.Withdrawal
                }).ToList()
            });
        }

        var html = WeeklySummaryEmailBuilder.Build(
            pref.User.Name, childSummaries, weekStart, weekEnd);

        await _emailService.SendEmailAsync(
            email,
            $"Juno Bank — Weekly Summary ({weekStart:dd MMM} - {weekEnd:dd MMM})",
            html);
    }

    private static TimeZoneInfo GetTimeZone(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch
        {
            return TimeZoneInfo.Utc;
        }
    }
}
