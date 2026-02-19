namespace JunoBank.Domain.Entities;

public class ScheduledAllowance
{
    public int Id { get; set; }

    // Link to Child
    public int ChildId { get; set; }
    public User Child { get; set; } = null!;

    // Created by Parent
    public int CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    // Allowance details
    public decimal Amount { get; set; }
    public AllowanceInterval Interval { get; set; } = AllowanceInterval.Weekly;
    public DayOfWeek DayOfWeek { get; set; }
    public int DayOfMonth { get; set; } = 1; // For Monthly/Yearly intervals
    public int MonthOfYear { get; set; } = 1; // For Yearly interval (1-12)
    public TimeOnly TimeOfDay { get; set; }
    public string Description { get; set; } = "Weekly Allowance";

    // Schedule tracking
    public DateTime NextRunDate { get; set; }
    public DateTime? LastRunDate { get; set; }

    // Timezone — IANA ID used to calculate next run dates in the user's local time
    public string TimeZoneId { get; set; } = "UTC";

    // Status
    public bool IsActive { get; set; } = true;

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
