namespace JunoBank.Domain.Entities;

public class PicturePassword
{
    public int Id { get; set; }

    // Link to User
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    // Hashed sequence of selected images (e.g., SHA256 of "cat,dog,star,moon")
    public string ImageSequenceHash { get; set; } = string.Empty;

    // Configuration
    public int GridSize { get; set; } = 9;
    public int SequenceLength { get; set; } = 4;

    // Security
    public int FailedAttempts { get; set; }
    public DateTime? LockedUntil { get; set; }
}
