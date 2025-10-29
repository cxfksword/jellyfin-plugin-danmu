using MediaBrowser.Controller.Library;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Jellyfin.Plugin.Danmu.Scrapers;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Subtitles;
using MediaBrowser.Controller.Persistence;

namespace Jellyfin.Plugin.Danmu
{
    /// <inheritdoc />
    public class ServiceRegistrator : IPluginServiceRegistrator
    {
        /// <inheritdoc />
        public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
        {
            serviceCollection.AddHostedService<PluginStartup>();

            serviceCollection.AddSingleton<ISubtitleProvider, DanmuSubtitleProvider>();
            serviceCollection.AddSingleton<Jellyfin.Plugin.Danmu.Core.IFileSystem>((ctx) =>
            {
                return new Jellyfin.Plugin.Danmu.Core.FileSystem();
            });
            serviceCollection.AddSingleton((ctx) =>
            {
                return new ScraperManager(ctx.GetRequiredService<ILoggerFactory>());
            });
            serviceCollection.AddSingleton((ctx) =>
            {
                return new LibraryManagerEventsHelper(ctx.GetRequiredService<IItemRepository>(), ctx.GetRequiredService<ILibraryManager>(), ctx.GetRequiredService<ILoggerFactory>(), ctx.GetRequiredService<Jellyfin.Plugin.Danmu.Core.IFileSystem>(), ctx.GetRequiredService<ScraperManager>());
            });
        }
    }
}
