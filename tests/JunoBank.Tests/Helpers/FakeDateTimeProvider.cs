using JunoBank.Web.Services;

namespace JunoBank.Tests.Helpers;

/// <summary>
/// Fake IDateTimeProvider for unit tests - allows controlling "current time".
/// </summary>
public class FakeDateTimeProvider : IDateTimeProvider
{
    private DateTime _now;

    public FakeDateTimeProvider(DateTime now)
    {
        _now = now;
    }

    public DateTime Now => _now;
    public DateTime UtcNow => _now.ToUniversalTime();

    /// <summary>
    /// Advance time by a specific amount.
    /// </summary>
    public void Advance(TimeSpan time) => _now = _now.Add(time);

    /// <summary>
    /// Set a specific time.
    /// </summary>
    public void SetNow(DateTime now) => _now = now;
}
