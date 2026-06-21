using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Jellyfin.Plugin.AudioDescription.Helpers;
using Microsoft.AspNetCore.Http;

namespace Jellyfin.Plugin.AudioDescription.Injection;

/// <summary>
/// Response-rewriting middleware that injects the badge CSS/JS into the web
/// client's index.html. The plugin is fully self-contained: instead of
/// registering a callback into the File Transformation plugin, it runs its own
/// middleware (inserted via <see cref="InjectionStartupFilter"/>).
/// </summary>
public class IndexInjectionMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexInjectionMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next delegate in the request pipeline.</param>
    public IndexInjectionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Invokes the middleware, rewriting the index.html response when applicable.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        // Only the SPA shell (index.html) needs rewriting; everything else passes
        // straight through with no buffering overhead.
        if (!IsIndexRequest(context.Request.Path))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        // Force a full, uncompressed 200 so there is a readable body to inject
        // into: drop Accept-Encoding (no gzip/br) and the conditional headers
        // (no 304-with-empty-body short-circuit by the static file middleware).
        context.Request.Headers.Remove("Accept-Encoding");
        context.Request.Headers.Remove("If-None-Match");
        context.Request.Headers.Remove("If-Modified-Since");

        var originalBody = context.Response.Body;
        using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        try
        {
            await _next(context).ConfigureAwait(false);

            buffer.Seek(0, SeekOrigin.Begin);
            var contentType = context.Response.ContentType ?? string.Empty;

            if (contentType.Contains("text/html", StringComparison.OrdinalIgnoreCase))
            {
                var html = await new StreamReader(buffer, Encoding.UTF8).ReadToEndAsync().ConfigureAwait(false);
                var injected = TransformationPatches.Inject(html);
                var bytes = Encoding.UTF8.GetBytes(injected);

                context.Response.ContentLength = bytes.Length;
                context.Response.Body = originalBody;
                await context.Response.Body.WriteAsync(bytes).ConfigureAwait(false);
            }
            else
            {
                // Path matched but the response was not HTML — copy through byte-identical.
                buffer.Seek(0, SeekOrigin.Begin);
                context.Response.Body = originalBody;
                await buffer.CopyToAsync(originalBody).ConfigureAwait(false);
            }
        }
        finally
        {
            context.Response.Body = originalBody;
        }
    }

    private static bool IsIndexRequest(PathString path)
    {
        if (!path.HasValue)
        {
            return true; // request for "/"
        }

        var value = path.Value!;
        return value == "/"
            || value.Equals("/web", StringComparison.OrdinalIgnoreCase)
            || value.Equals("/web/", StringComparison.OrdinalIgnoreCase)
            || value.EndsWith("/index.html", StringComparison.OrdinalIgnoreCase);
    }
}
