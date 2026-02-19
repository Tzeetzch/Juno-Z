namespace JunoBank.Application.DTOs;

public class ParentDashboardData
{
    public string ChildName { get; set; } = string.Empty;
    public decimal ChildBalance { get; set; }
    public int PendingRequestCount { get; set; }
}

public class ChildDashboardData
{
    public string Name { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public List<Transaction> RecentTransactions { get; set; } = new();
    public List<MoneyRequest> RecentRequests { get; set; } = new();
}

/// <summary>
/// Summary DTO for displaying child cards on parent dashboard.
/// </summary>
public class ChildSummary
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public int PendingRequestCount { get; set; }
}

/// <summary>
/// Summary DTO for displaying parents in user management.
/// </summary>
public class ParentSummary
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
}

public class ChildLockoutStatus
{
    public bool IsLocked { get; set; }
    public DateTime? LockedUntil { get; set; }
    public int FailedAttempts { get; set; }
}
