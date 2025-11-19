using System;
using Jellyfin.Plugin.Danmu.Controllers.Entity;
using Jellyfin.Plugin.Danmu.Core;
using Jellyfin.Plugin.Danmu.Scrapers;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Subtitles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
            serviceCollection.AddSingleton<IFileSystem>(_ => new FileSystem());
            serviceCollection.AddSingleton((ctx) =>
            {
                return new ScraperManager(ctx.GetRequiredService<ILoggerFactory>());
            });
            serviceCollection.AddSingleton((ctx) =>
            {
                return new LibraryManagerEventsHelper(ctx.GetRequiredService<IItemRepository>(), ctx.GetRequiredService<ILibraryManager>(), ctx.GetRequiredService<ILoggerFactory>(), ctx.GetRequiredService<IFileSystem>(), ctx.GetRequiredService<ScraperManager>());
            });
            serviceCollection.AddSingleton<FileCache<AnimeCacheItem>>((ctx) =>
            {
                var applicationPaths = ctx.GetRequiredService<IApplicationPaths>();
                return new FileCache<AnimeCacheItem>(applicationPaths, ctx.GetRequiredService<ILoggerFactory>(),TimeSpan.FromDays(31), TimeSpan.FromSeconds(60));
            });
        }
    }
}
