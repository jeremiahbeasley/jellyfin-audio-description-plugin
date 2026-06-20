using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Jellyfin.Plugin.AudioDescription.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AudioDescription.Api;

/// <summary>
/// API controller that exposes a batch endpoint the web client uses to determine
/// which items carry an Audio Description audio track.
/// </summary>
[ApiController]
[Authorize]
[Route("AudioDescription")]
public class AudioDescriptionController : ControllerBase
{
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<AudioDescriptionController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioDescriptionController"/> class.
    /// </summary>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="logger">The logger.</param>
    public AudioDescriptionController(
        ILibraryManager libraryManager,
        ILogger<AudioDescriptionController> logger)
    {
        _libraryManager = libraryManager;
        _logger = logger;
    }

    /// <summary>
    /// Checks a batch of item IDs and returns those that have an Audio Description audio track.
    /// </summary>
    /// <param name="ids">Comma-separated Jellyfin item GUIDs.</param>
    /// <returns>
    /// A dictionary mapping each matching item ID to the title of its Audio Description track.
    /// Items without an AD track are omitted from the response.
    /// </returns>
    [HttpGet("Check")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<Dictionary<string, string>> CheckItems([FromQuery][Required] string ids)
    {
        var plugin = Plugin.Instance;
        if (plugin is null)
        {
            return new Dictionary<string, string>();
        }

        var keywords = (plugin.Configuration.AudioDescriptionKeywords ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawId in ids.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!Guid.TryParse(rawId, out var itemId))
            {
                continue;
            }

            var item = _libraryManager.GetItemById(itemId);
            if (item is null)
            {
                continue;
            }

            var adTrack = item.GetMediaStreams()
                .Where(s => s.Type == MediaStreamType.Audio)
                .FirstOrDefault(s => MatchesKeyword(s, keywords));

            if (adTrack is not null)
            {
                var trackTitle = adTrack.DisplayTitle
                    ?? adTrack.Title
                    ?? adTrack.Language
                    ?? "Audio Description";
                result[rawId] = trackTitle;
            }
        }

        return result;
    }

    /// <summary>
    /// Returns the current plugin configuration relevant to the web client.
    /// </summary>
    [HttpGet("Config")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<object> GetConfig()
    {
        var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
        return new
        {
            badgePosition = config.BadgePosition.ToString(),
            showBadgeLabel = config.ShowBadgeLabel
        };
    }

    private static bool MatchesKeyword(MediaStream stream, string[] keywords)
    {
        foreach (var kw in keywords)
        {
            if (Contains(stream.Title, kw)
                || Contains(stream.DisplayTitle, kw)
                || Contains(stream.Language, kw))
            {
                return true;
            }
        }

        return false;
    }

    private static bool Contains(string? source, string value)
        => source is not null
           && source.Contains(value, StringComparison.OrdinalIgnoreCase);
}
