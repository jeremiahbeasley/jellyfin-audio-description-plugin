using System;
using System.Reflection;

namespace Jellyfin.Plugin.AudioDescription.Helpers;

/// <summary>
/// Injects the badge stylesheet and script into the Jellyfin web client's
/// index.html. Mirrors the self-contained injection pattern used by the A11y
/// Book Reader plugin: no dependency on the File Transformation plugin.
/// </summary>
public static class TransformationPatches
{
    private const string Marker = "/AudioDescription/audioDescription.js";

    /// <summary>
    /// Returns <paramref name="html"/> with the badge CSS/JS tags inserted before
    /// <c>&lt;/head&gt;</c> and <c>&lt;/body&gt;</c>. Idempotent — if the marker is
    /// already present (page already injected) the input is returned unchanged.
    /// </summary>
    public static string Inject(string html)
    {
        if (string.IsNullOrEmpty(html) || html.Contains(Marker, StringComparison.Ordinal))
        {
            return html;
        }

        var version = typeof(TransformationPatches).Assembly.GetName().Version?.ToString() ?? "1.0.0.0";
        var v = "?v=" + version;
        var css = "<link rel=\"stylesheet\" href=\"/AudioDescription/audioDescription.css" + v + "\" />";
        var script = "<script defer src=\"/AudioDescription/audioDescription.js" + v + "\"></script>";

        return html
            .Replace("</head>", css + "</head>", StringComparison.Ordinal)
            .Replace("</body>", script + "</body>", StringComparison.Ordinal);
    }
}
