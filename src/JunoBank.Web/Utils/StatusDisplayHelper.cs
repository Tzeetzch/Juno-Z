using JunoBank.Web.Data.Entities;

namespace JunoBank.Web.Utils;

/// <summary>
/// Centralized display helpers for request status formatting.
/// Used in child dashboard and parent request views.
/// </summary>
public static class StatusDisplayHelper
{
    /// <summary>
    /// Gets human-readable status text with emoji indicator.
    /// </summary>
    public static string GetStatusText(RequestStatus status) => status switch
    {
        RequestStatus.Pending => "Waiting",
        RequestStatus.Approved => "Approved",
        RequestStatus.Denied => "Denied",
        _ => ""
    };

    /// <summary>
    /// Gets the appropriate color for status display.
    /// Uses Material Design color palette.
    /// </summary>
    public static string GetStatusColor(RequestStatus status) => status switch
    {
        RequestStatus.Pending => "#FFA726",   // Orange
        RequestStatus.Approved => "#4caf50",  // Green
        RequestStatus.Denied => "#f44336",    // Red
        _ => "inherit"
    };
}
