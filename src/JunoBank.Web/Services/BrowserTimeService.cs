using Microsoft.JSInterop;

namespace JunoBank.Web.Services;

/// <summary>
/// Scoped service that detects the browser's IANA timezone and converts between UTC and local time.
/// </summary>
public class BrowserTimeService : IBrowserTimeService
{
    private TimeZoneInfo? _timeZone;

    public string? TimeZoneId { get; private set; }

    public async Task InitializeAsync(IJSRuntime js)
    {
        if (TimeZoneId != null) return; // Already initialized

        try
        {
            var tzId = await js.InvokeAsync<string>("getTimeZone");
            if (!string.IsNullOrEmpty(tzId))
            {
                TimeZoneId = tzId;
                _timeZone = TimeZoneInfo.FindSystemTimeZoneById(tzId);
            }
        }
        catch
        {
            // JS interop can fail during prerender or if browser doesn't support Intl.
            // Fall back to UTC silently.
        }
    }

    public DateTime ToLocal(DateTime utc)
    {
        if (_timeZone == null)
            return utc;

        var utcTime = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(utcTime, _timeZone);
    }

    public DateTime ToUtc(DateTime local)
    {
        if (_timeZone == null)
            return local;

        var unspecified = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(unspecified, _timeZone);
    }
}
