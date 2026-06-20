using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
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
        // Add any future scoped services here.
    }
}
