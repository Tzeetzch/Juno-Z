using Microsoft.JSInterop;

namespace JunoBank.Web.Services;

/// <summary>
/// Provides browser timezone conversion for displaying UTC dates in the user's local time.
/// Scoped service — initialized once per circuit via JS interop.
/// </summary>
public interface IBrowserTimeService
{
    /// <summary>
    /// IANA timezone ID detected from the browser (e.g. "Europe/Amsterdam").
    /// Null until InitializeAsync is called.
    /// </summary>
    string? TimeZoneId { get; }

    /// <summary>
    /// Detect the browser's timezone via JS interop. Call once in MainLayout OnAfterRenderAsync.
    /// </summary>
    Task InitializeAsync(IJSRuntime js);

    /// <summary>
    /// Convert a UTC DateTime to the user's local time.
    /// Falls back to UTC if timezone is not yet initialized.
    /// </summary>
    DateTime ToLocal(DateTime utc);

    /// <summary>
    /// Convert a local DateTime (in the user's timezone) to UTC.
    /// Falls back to returning the input unchanged if timezone is not yet initialized.
    /// </summary>
    DateTime ToUtc(DateTime local);
}
