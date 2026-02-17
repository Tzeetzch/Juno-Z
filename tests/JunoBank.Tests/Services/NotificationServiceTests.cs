using JunoBank.Tests.Helpers;
using JunoBank.Web.Data.Entities;
using JunoBank.Web.Services;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace JunoBank.Tests.Services;

public class NotificationServiceTests : DatabaseTestBase
{
    private readonly FakeTimeProvider _timeProvider;
    private readonly Mock<IEmailService> _emailService;
    private readonly Mock<IEmailConfigService> _emailConfigService;
    private readonly NotificationService _service;

    public NotificationServiceTests()
    {
        // Sunday, Feb 15 2026, 08:02 UTC
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 2, 15, 8, 2, 0, TimeSpan.Zero));
        _emailService = new Mock<IEmailService>();
        _emailConfigService = new Mock<IEmailConfigService>();
        _emailConfigService.Setup(e => e.IsConfigured).Returns(true);
        _emailService.Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        _service = new NotificationService(
            Db, _timeProvider, _emailService.Object, _emailConfigService.Object,
            CreateLogger<NotificationService>());
    }

    private async Task<User> CreateParent(string name = "Dad", string email = "dad@test.com")
    {
        var parent = new User
        {
            Name = name,
            Email = email,
            Role = UserRole.Parent,
            IsAdmin = true,
            PasswordHash = "hash",
            Balance = 0,
            CreatedAt = DateTime.UtcNow
        };
        Db.Users.Add(parent);
        await Db.SaveChangesAsync();
        return parent;
    }

    private async Task<User> CreateChild(string name = "Junior", decimal balance = 25.00m)
    {
        var child = new User
        {
            Name = name,
            Role = UserRole.Child,
            Balance = balance,
            CreatedAt = DateTime.UtcNow
        };
        Db.Users.Add(child);
        await Db.SaveChangesAsync();
        return child;
    }

    #region GetPreferenceAsync Tests

    [Fact]
    public async Task GetPreferenceAsync_ReturnsNull_WhenNoneExists()
    {
        var parent = await CreateParent();

        var result = await _service.GetPreferenceAsync(parent.Id, NotificationType.WeeklySummary);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetPreferenceAsync_ReturnsPreference_WhenExists()
    {
        var parent = await CreateParent();
        Db.NotificationPreferences.Add(new NotificationPreference
        {
            UserId = parent.Id,
            Type = NotificationType.WeeklySummary,
            Enabled = true,
            DayOfWeek = DayOfWeek.Monday,
            TimeOfDay = new TimeOnly(9, 0)
        });
        await Db.SaveChangesAsync();

        var result = await _service.GetPreferenceAsync(parent.Id, NotificationType.WeeklySummary);

        Assert.NotNull(result);
        Assert.True(result.Enabled);
        Assert.Equal(DayOfWeek.Monday, result.DayOfWeek);
    }

    #endregion

    #region UpsertPreferenceAsync Tests

    [Fact]
    public async Task UpsertPreferenceAsync_CreatesNew_WhenNoneExists()
    {
        var parent = await CreateParent();

        var result = await _service.UpsertPreferenceAsync(new NotificationPreference
        {
            UserId = parent.Id,
            Type = NotificationType.WeeklySummary,
            Enabled = true,
            DayOfWeek = DayOfWeek.Sunday,
            TimeOfDay = new TimeOnly(8, 0),
            TimeZoneId = "Europe/Amsterdam"
        });

        Assert.True(result.Enabled);
        Assert.Equal(1, Db.NotificationPreferences.Count());
    }

    [Fact]
    public async Task UpsertPreferenceAsync_UpdatesExisting()
    {
        var parent = await CreateParent();
        Db.NotificationPreferences.Add(new NotificationPreference
        {
            UserId = parent.Id,
            Type = NotificationType.WeeklySummary,
            Enabled = false,
            DayOfWeek = DayOfWeek.Monday
        });
        await Db.SaveChangesAsync();

        await _service.UpsertPreferenceAsync(new NotificationPreference
        {
            UserId = parent.Id,
            Type = NotificationType.WeeklySummary,
            Enabled = true,
            DayOfWeek = DayOfWeek.Friday,
            TimeOfDay = new TimeOnly(20, 0),
            TimeZoneId = "UTC"
        });

        var pref = Db.NotificationPreferences.Single();
        Assert.True(pref.Enabled);
        Assert.Equal(DayOfWeek.Friday, pref.DayOfWeek);
        Assert.Equal(new TimeOnly(20, 0), pref.TimeOfDay);
    }

    #endregion

    #region IsDueToSend Tests

    [Fact]
    public void IsDueToSend_ReturnsTrue_WhenScheduleMatches()
    {
        // Sunday 08:02 UTC, preference for Sunday 08:00 UTC
        var pref = new NotificationPreference
        {
            DayOfWeek = DayOfWeek.Sunday,
            TimeOfDay = new TimeOnly(8, 0),
            TimeZoneId = "UTC",
            LastSentDate = null
        };

        Assert.True(NotificationService.IsDueToSend(pref, new DateTime(2026, 2, 15, 8, 2, 0)));
    }

    [Fact]
    public void IsDueToSend_ReturnsFalse_WrongDayOfWeek()
    {
        var pref = new NotificationPreference
        {
            DayOfWeek = DayOfWeek.Monday,
            TimeOfDay = new TimeOnly(8, 0),
            TimeZoneId = "UTC",
            LastSentDate = null
        };

        // Feb 15 2026 is Sunday, not Monday
        Assert.False(NotificationService.IsDueToSend(pref, new DateTime(2026, 2, 15, 8, 2, 0)));
    }

    [Fact]
    public void IsDueToSend_ReturnsFalse_TooEarly()
    {
        var pref = new NotificationPreference
        {
            DayOfWeek = DayOfWeek.Sunday,
            TimeOfDay = new TimeOnly(8, 0),
            TimeZoneId = "UTC",
            LastSentDate = null
        };

        // 07:50 — 10 minutes before scheduled time
        Assert.False(NotificationService.IsDueToSend(pref, new DateTime(2026, 2, 15, 7, 50, 0)));
    }

    [Fact]
    public void IsDueToSend_ReturnsFalse_TooLate()
    {
        var pref = new NotificationPreference
        {
            DayOfWeek = DayOfWeek.Sunday,
            TimeOfDay = new TimeOnly(8, 0),
            TimeZoneId = "UTC",
            LastSentDate = null
        };

        // 08:15 — more than 10 minutes after
        Assert.False(NotificationService.IsDueToSend(pref, new DateTime(2026, 2, 15, 8, 15, 0)));
    }

    [Fact]
    public void IsDueToSend_ReturnsFalse_AlreadySentThisWeek()
    {
        var pref = new NotificationPreference
        {
            DayOfWeek = DayOfWeek.Sunday,
            TimeOfDay = new TimeOnly(8, 0),
            TimeZoneId = "UTC",
            LastSentDate = new DateTime(2026, 2, 10, 8, 0, 0) // 5 days ago (< 6)
        };

        Assert.False(NotificationService.IsDueToSend(pref, new DateTime(2026, 2, 15, 8, 2, 0)));
    }

    [Fact]
    public void IsDueToSend_ReturnsTrue_LastSentOverAWeekAgo()
    {
        var pref = new NotificationPreference
        {
            DayOfWeek = DayOfWeek.Sunday,
            TimeOfDay = new TimeOnly(8, 0),
            TimeZoneId = "UTC",
            LastSentDate = new DateTime(2026, 2, 8, 8, 0, 0) // 7 days ago (>= 6)
        };

        Assert.True(NotificationService.IsDueToSend(pref, new DateTime(2026, 2, 15, 8, 2, 0)));
    }

    [Fact]
    public void IsDueToSend_RespectsTimezone()
    {
        // UTC time is 07:02, but in Europe/Amsterdam (UTC+1) it's 08:02
        var pref = new NotificationPreference
        {
            DayOfWeek = DayOfWeek.Sunday,
            TimeOfDay = new TimeOnly(8, 0),
            TimeZoneId = "Europe/Amsterdam",
            LastSentDate = null
        };

        Assert.True(NotificationService.IsDueToSend(pref, new DateTime(2026, 2, 15, 7, 2, 0)));
    }

    #endregion

    #region ProcessDueNotificationsAsync Tests

    [Fact]
    public async Task ProcessDueNotificationsAsync_ReturnsZero_WhenSmtpNotConfigured()
    {
        _emailConfigService.Setup(e => e.IsConfigured).Returns(false);

        var result = await _service.ProcessDueNotificationsAsync();

        Assert.Equal(0, result);
        _emailService.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ProcessDueNotificationsAsync_SendsEmail_WhenDue()
    {
        var parent = await CreateParent();
        var child = await CreateChild();

        // Add a transaction
        Db.Transactions.Add(new Transaction
        {
            UserId = child.Id,
            Amount = 5.00m,
            Type = TransactionType.Deposit,
            Description = "Chores",
            IsApproved = true,
            CreatedAt = new DateTime(2026, 2, 14, 10, 0, 0)
        });

        Db.NotificationPreferences.Add(new NotificationPreference
        {
            UserId = parent.Id,
            Type = NotificationType.WeeklySummary,
            Enabled = true,
            DayOfWeek = DayOfWeek.Sunday,
            TimeOfDay = new TimeOnly(8, 0),
            TimeZoneId = "UTC"
        });
        await Db.SaveChangesAsync();

        var result = await _service.ProcessDueNotificationsAsync();

        Assert.Equal(1, result);
        _emailService.Verify(e => e.SendEmailAsync(
            "dad@test.com",
            It.Is<string>(s => s.Contains("Weekly Summary")),
            It.Is<string>(s => s.Contains("Junior") && s.Contains("Chores"))),
            Times.Once);
    }

    [Fact]
    public async Task ProcessDueNotificationsAsync_SkipsDisabled()
    {
        var parent = await CreateParent();
        Db.NotificationPreferences.Add(new NotificationPreference
        {
            UserId = parent.Id,
            Type = NotificationType.WeeklySummary,
            Enabled = false,
            DayOfWeek = DayOfWeek.Sunday,
            TimeOfDay = new TimeOnly(8, 0),
            TimeZoneId = "UTC"
        });
        await Db.SaveChangesAsync();

        var result = await _service.ProcessDueNotificationsAsync();

        Assert.Equal(0, result);
        _emailService.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ProcessDueNotificationsAsync_UpdatesLastSentDate()
    {
        var parent = await CreateParent();
        Db.NotificationPreferences.Add(new NotificationPreference
        {
            UserId = parent.Id,
            Type = NotificationType.WeeklySummary,
            Enabled = true,
            DayOfWeek = DayOfWeek.Sunday,
            TimeOfDay = new TimeOnly(8, 0),
            TimeZoneId = "UTC"
        });
        await Db.SaveChangesAsync();

        await _service.ProcessDueNotificationsAsync();

        var pref = Db.NotificationPreferences.Single();
        Assert.NotNull(pref.LastSentDate);
    }

    #endregion
}

public class WeeklySummaryEmailBuilderTests
{
    [Fact]
    public void Build_ContainsParentName()
    {
        var html = WeeklySummaryEmailBuilder.Build(
            "Dad",
            new List<ChildWeeklySummary>(),
            new DateTime(2026, 2, 8),
            new DateTime(2026, 2, 15));

        Assert.Contains("Dad", html);
        Assert.Contains("08 Feb", html);
        Assert.Contains("15 Feb 2026", html);
    }

    [Fact]
    public void Build_ContainsChildDetails()
    {
        var children = new List<ChildWeeklySummary>
        {
            new()
            {
                Name = "Junior",
                Balance = 25.50m,
                BalanceChange = 5.00m,
                PendingRequests = 2,
                Transactions = new List<TransactionSummary>
                {
                    new() { CreatedAt = new DateTime(2026, 2, 14), Description = "Chores", Amount = 5.00m, IsDeposit = true }
                }
            }
        };

        var html = WeeklySummaryEmailBuilder.Build("Dad", children, new DateTime(2026, 2, 8), new DateTime(2026, 2, 15));

        Assert.Contains("Junior", html);
        Assert.Contains("25.50", html);
        Assert.Contains("5.00", html);
        Assert.Contains("Chores", html);
        Assert.Contains("2 pending request(s)", html);
    }

    [Fact]
    public void Build_ShowsNoTransactionsMessage()
    {
        var children = new List<ChildWeeklySummary>
        {
            new() { Name = "Junior", Balance = 10.00m, BalanceChange = 0, Transactions = new() }
        };

        var html = WeeklySummaryEmailBuilder.Build("Dad", children, new DateTime(2026, 2, 8), new DateTime(2026, 2, 15));

        Assert.Contains("No transactions this week", html);
    }

    [Fact]
    public void Build_HtmlEncodesNames()
    {
        var html = WeeklySummaryEmailBuilder.Build(
            "<script>alert('xss')</script>",
            new List<ChildWeeklySummary>(),
            new DateTime(2026, 2, 8),
            new DateTime(2026, 2, 15));

        Assert.DoesNotContain("<script>", html);
        Assert.Contains("&lt;script&gt;", html);
    }
}
