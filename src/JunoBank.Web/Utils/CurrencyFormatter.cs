using System.Globalization;

namespace JunoBank.Web.Utils;

/// <summary>
/// Centralized currency formatting for consistent display across the application.
/// Uses InvariantCulture to ensure € formatting works correctly regardless of locale.
/// </summary>
public static class CurrencyFormatter
{
    /// <summary>
    /// Formats a decimal amount as Euro currency (e.g., "€10.00")
    /// </summary>
    public static string Format(decimal amount)
    {
        return $"€{amount:0.00}";
    }

    /// <summary>
    /// Formats a decimal amount with sign indicator (e.g., "+€10.00" or "-€5.00")
    /// </summary>
    public static string FormatWithSign(decimal amount, bool isDeduction = false)
    {
        var sign = isDeduction ? "-" : "+";
        return $"{sign}€{amount:0.00}";
    }

    /// <summary>
    /// Formats using invariant culture for data consistency.
    /// </summary>
    public static string FormatInvariant(decimal amount)
    {
        return amount.ToString("0.00", CultureInfo.InvariantCulture);
    }
}
