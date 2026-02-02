namespace JunoBank.Web.Constants;

/// <summary>
/// Centralized constants for picture password functionality.
/// Used by child login and picture password setup.
/// </summary>
public static class PicturePasswordImages
{
    /// <summary>
    /// All available picture identifiers for the picture grid.
    /// </summary>
    public static readonly string[] AllImages =
    {
        "cat", "dog", "star", "moon", "sun", "tree",
        "fish", "bird", "car", "flower", "heart", "apple"
    };

    /// <summary>
    /// Number of images to display in the grid (3x3 grid).
    /// </summary>
    public const int GridDisplayCount = 9;

    /// <summary>
    /// Default required sequence length for picture password.
    /// </summary>
    public const int DefaultSequenceLength = 4;

    /// <summary>
    /// Gets the emoji representation of a picture identifier.
    /// </summary>
    public static string GetEmoji(string image) => image switch
    {
        "cat" => "🐱",
        "dog" => "🐶",
        "star" => "⭐",
        "moon" => "🌙",
        "sun" => "☀️",
        "tree" => "🌳",
        "fish" => "🐟",
        "bird" => "🐦",
        "car" => "🚗",
        "flower" => "🌸",
        "heart" => "❤️",
        "apple" => "🍎",
        _ => "❓"
    };
}
