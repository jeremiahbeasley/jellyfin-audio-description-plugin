using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.AudioDescription.Configuration;

/// <summary>
/// Plugin configuration for Audio Description Badges.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class
    /// with sensible defaults.
    /// </summary>
    public PluginConfiguration()
    {
        // Default keywords used to identify Audio Description audio tracks.
        // Comma-separated; checked case-insensitively against track Title/Language fields.
        AudioDescriptionKeywords = "audio description,descriptive audio,described video,visually impaired,AD,VI";

        BadgePosition = BadgeCorner.TopLeft;
        ShowBadgeLabel = true;
    }

    /// <summary>
    /// Gets or sets the comma-separated list of keywords used to identify
    /// Audio Description audio tracks.
    /// </summary>
    public string AudioDescriptionKeywords { get; set; }

    /// <summary>
    /// Gets or sets the corner of the card thumbnail where the badge appears.
    /// </summary>
    public BadgeCorner BadgePosition { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the badge shows a text label
    /// ("AD") in addition to the icon. Recommended for screen-reader clarity.
    /// </summary>
    public bool ShowBadgeLabel { get; set; }
}

/// <summary>
/// Corner position for the Audio Description badge overlay.
/// </summary>
public enum BadgeCorner
{
    /// <summary>Top-left corner of the card.</summary>
    TopLeft,

    /// <summary>Top-right corner of the card.</summary>
    TopRight,

    /// <summary>Bottom-left corner of the card.</summary>
    BottomLeft,

    /// <summary>Bottom-right corner of the card.</summary>
    BottomRight
}
