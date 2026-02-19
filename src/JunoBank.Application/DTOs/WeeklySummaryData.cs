namespace JunoBank.Application.DTOs;

public class ChildWeeklySummary
{
    public required string Name { get; set; }
    public decimal Balance { get; set; }
    public decimal BalanceChange { get; set; }
    public int PendingRequests { get; set; }
    public List<TransactionSummary> Transactions { get; set; } = new();
}

public class TransactionSummary
{
    public DateTime CreatedAt { get; set; }
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
    public bool IsDeposit { get; set; }
}
