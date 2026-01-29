namespace JunoBank.Web.Data.Entities;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public UserRole Role { get; set; }

    // Parent authentication
    public string? Email { get; set; }
    public string? PasswordHash { get; set; }

    // Child authentication
    public PicturePassword? PicturePassword { get; set; }

    // Balance
    public decimal Balance { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<MoneyRequest> MoneyRequests { get; set; } = new List<MoneyRequest>();
}

public enum UserRole
{
    Parent,
    Child
}
