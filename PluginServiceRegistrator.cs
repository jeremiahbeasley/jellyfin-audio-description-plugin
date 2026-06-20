using Jellyfin.Plugin.AudioDescription.Api;
using MediaBrowser.Common.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.AudioDescription;

/// <summary>
/// Registers services for the Audio Description plugin.
/// </summary>
public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServiceProvider applicationServiceProvider)
    {
        // AudioDescriptionController is picked up automatically by Jellyfin's
        // controller discovery â no explicit registration needed.
        // Add any future scoped services here.
    }
}
