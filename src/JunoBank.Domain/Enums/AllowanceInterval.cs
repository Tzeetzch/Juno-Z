namespace JunoBank.Domain.Enums;

/// <summary>
/// Defines how often an allowance is paid out.
/// </summary>
public enum AllowanceInterval
{
    /// <summary>Every hour (for testing)</summary>
    Hourly = 0,

    /// <summary>Every day at the specified time</summary>
    Daily = 1,

    /// <summary>Every week on the specified day and time</summary>
    Weekly = 2,

    /// <summary>Every month on the specified day and time</summary>
    Monthly = 3,

    /// <summary>Every year on the specified date and time</summary>
    Yearly = 4
}
