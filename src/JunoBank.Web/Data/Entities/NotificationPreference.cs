namespace JunoBank.Web.Data.Entities;

public class NotificationPreference
{
    public int Id { get; set; }

    // Which parent this belongs to
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public NotificationType Type { get; set; } = NotificationType.WeeklySummary;
    public bool Enabled { get; set; }

    // Schedule
    public DayOfWeek DayOfWeek { get; set; } = DayOfWeek.Sunday;
    public TimeOnly TimeOfDay { get; set; } = new TimeOnly(8, 0);

    // IANA timezone — same pattern as ScheduledAllowance.TimeZoneId
    public string TimeZoneId { get; set; } = "UTC";

    // Tracking
    public DateTime? LastSentDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum NotificationType
{
    WeeklySummary = 0
}
