using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MediaBrowser.Controller.Persistence;
using Jellyfin.Plugin.Danmu.Scrapers;
using Jellyfin.Plugin.Danmu.Scrapers.Bilibili;
using Jellyfin.Plugin.Danmu.Scrapers.Dandan;
using MediaBrowser.Common;

namespace Jellyfin.Plugin.Danmu
{
    /// <inheritdoc />
    public class ServiceRegistrator : IPluginServiceRegistrator
    {
        /// <inheritdoc />
        public void RegisterServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<Jellyfin.Plugin.Danmu.Core.IFileSystem>((ctx) =>
            {
                return new Jellyfin.Plugin.Danmu.Core.FileSystem();
            });
            serviceCollection.AddSingleton<ScraperManager>((ctx) =>
            {
                return new ScraperManager(ctx.GetRequiredService<ILoggerFactory>());
            });
            serviceCollection.AddSingleton<LibraryManagerEventsHelper>((ctx) =>
            {
                return new LibraryManagerEventsHelper(ctx.GetRequiredService<ILibraryManager>(), ctx.GetRequiredService<ILoggerFactory>(), ctx.GetRequiredService<Jellyfin.Plugin.Danmu.Core.IFileSystem>(), ctx.GetRequiredService<ScraperManager>());
            });

        }
    }
}
