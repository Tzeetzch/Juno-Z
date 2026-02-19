namespace JunoBank.Domain.Entities;

public class MoneyRequest
{
    public int Id { get; set; }

    // Link to Child
    public int ChildId { get; set; }
    public User Child { get; set; } = null!;

    // Request details
    public decimal Amount { get; set; }
    public RequestType Type { get; set; }
    public string Description { get; set; } = string.Empty;

    // Status
    public RequestStatus Status { get; set; } = RequestStatus.Pending;

    // Resolution
    public int? ResolvedByUserId { get; set; }
    public User? ResolvedByUser { get; set; }
    public string? ParentNote { get; set; }
    public DateTime? ResolvedAt { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
