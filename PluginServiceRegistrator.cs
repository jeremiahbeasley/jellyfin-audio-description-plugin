using Jellyfin.Plugin.AudioDescription.Injection;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.AudioDescription;

/// <summary>
/// Registers services for the Audio Description plugin.
/// </summary>
public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        // AudioDescriptionController is picked up automatically by Jellyfin's
        // controller discovery — no explicit registration needed.

        // Self-contained UI injection: run our own response-rewriting middleware
        // (no dependency on the File Transformation plugin) to add the badge
        // CSS/JS to the web client's index.html.
        serviceCollection.AddSingleton<IStartupFilter, InjectionStartupFilter>();
    }
}
