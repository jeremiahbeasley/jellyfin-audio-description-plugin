using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
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

    /// <summary>
    /// Serves the embedded badge script to the web client. Anonymous because a
    /// plain <c>&lt;script src&gt;</c> tag cannot send a Jellyfin auth token.
    /// </summary>
    [HttpGet("audioDescription.js")]
    [AllowAnonymous]
    [Produces("application/javascript")]
    public ActionResult GetScript() => ServeEmbedded("Web.audioDescription.js", "application/javascript");

    /// <summary>
    /// Serves the embedded badge stylesheet to the web client.
    /// </summary>
    [HttpGet("audioDescription.css")]
    [AllowAnonymous]
    [Produces("text/css")]
    public ActionResult GetStylesheet() => ServeEmbedded("Web.audioDescription.css", "text/css");

    private ActionResult ServeEmbedded(string resourceName, string contentType)
    {
        var stream = typeof(Plugin).Assembly
            .GetManifestResourceStream($"{typeof(Plugin).Namespace}.{resourceName}");
        if (stream is null)
        {
            return NotFound();
        }

        return File(stream, contentType);
    }

    private static bool MatchesKeyword(MediaStream stream, string[] keywords)
    {
        foreach (var kw in keywords)
        {
            if (MatchesWord(stream.Title, kw)
                || MatchesWord(stream.DisplayTitle, kw)
                || MatchesWord(stream.Language, kw))
            {
                return true;
            }
        }

        return false;
    }

    // Whole-word, case-insensitive match. Word boundaries (not a raw substring)
    // stop short keywords like "AD"/"VI" from matching inside ordinary words —
    // e.g. "AD" must not match "Brad" in a commentary credit, and "VI" must not
    // match "movie" or the language code "vie". A standalone "AD" token still matches.
    private static bool MatchesWord(string? source, string value)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(value))
        {
            return false;
        }

        var pattern = $"\\b{Regex.Escape(value)}\\b";
        return Regex.IsMatch(source, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }
}
