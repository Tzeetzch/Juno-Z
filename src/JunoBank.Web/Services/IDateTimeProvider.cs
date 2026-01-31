namespace JunoBank.Web.Services;

/// <summary>
/// Abstraction for DateTime.Now to enable unit testing with controlled time.
/// </summary>
public interface IDateTimeProvider
{
    DateTime Now { get; }
    DateTime UtcNow { get; }
}

/// <summary>
/// Default implementation using system clock.
/// </summary>
public class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime Now => DateTime.Now;
    public DateTime UtcNow => DateTime.UtcNow;
}
