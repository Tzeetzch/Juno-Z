namespace JunoBank.Application.DTOs;

public record UserSession
{
    public int UserId { get; init; }
    public string UserName { get; init; } = string.Empty;
    public UserRole Role { get; init; }
}
