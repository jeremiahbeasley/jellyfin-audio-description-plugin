using System;
using System.Collections.Generic;
using Jellyfin.Plugin.AudioDescription.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.AudioDescription;

/// <summary>
/// The Audio Description plugin for Jellyfin.
/// Surfaces a visual and screen-reader-accessible badge on library cards
/// whenever a media item carries an Audio Description audio track.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Plugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
    /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    /// <inheritdoc />
    public override string Name => "Audio Description Badges";

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("3c8f7e2a-d4b9-4e6f-a1c5-8d9e0b7a6c4d");

    /// <inheritdoc />
    public override string Description =>
        "Displays a visual and screen-reader-accessible badge on library cards " +
        "for media that contains an Audio Description audio track.";

    /// <summary>
    /// Gets the current plugin instance.
    /// </summary>
    public static Plugin? Instance { get; private set; }

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        return new[]
        {
            new PluginPageInfo
            {
                Name = Name,
                EmbeddedResourcePath = $"{GetType().Namespace}.Web.configPage.html",
                EnableInMainMenu = true
            }
        };
    }
}
