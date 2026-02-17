using JunoBank.Web.Data.Entities;

namespace JunoBank.Web.Auth;

public record UserSession
{
    public int UserId { get; init; }
    public string UserName { get; init; } = string.Empty;
    public UserRole Role { get; init; }
}
