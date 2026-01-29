namespace JunoBank.Web.Data.Entities;

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
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly TimeOfDay { get; set; }

    // Schedule tracking
    public DateTime NextRunDate { get; set; }
    public DateTime? LastRunDate { get; set; }

    // Status
    public bool IsActive { get; set; } = true;

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
