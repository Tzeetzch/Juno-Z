namespace JunoBank.Web.Data.Entities;

public class Transaction
{
    public int Id { get; set; }

    // Link to User (account owner)
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    // Transaction details
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public string Description { get; set; } = string.Empty;

    // Approval (for child transactions)
    public bool IsApproved { get; set; }
    public int? ApprovedByUserId { get; set; }
    public User? ApprovedByUser { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum TransactionType
{
    Deposit,
    Withdrawal,
    Allowance
}
