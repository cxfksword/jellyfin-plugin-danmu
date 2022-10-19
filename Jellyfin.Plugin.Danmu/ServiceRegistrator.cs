using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jellyfin.Plugin.Danmu.Api;
using Jellyfin.Plugin.Danmu.Providers;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MediaBrowser.Controller.Persistence;

namespace Jellyfin.Plugin.Danmu
{
    /// <inheritdoc />
    public class ServiceRegistrator : IPluginServiceRegistrator
    {
        /// <inheritdoc />
        public void RegisterServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<BilibiliApi>((ctx) =>
            {
                return new BilibiliApi(ctx.GetRequiredService<ILoggerFactory>());
            });
            serviceCollection.AddSingleton<LibraryManagerEventsHelper>((ctx) =>
            {
                return new LibraryManagerEventsHelper(ctx.GetRequiredService<ILibraryManager>(), ctx.GetRequiredService<ILoggerFactory>(), ctx.GetRequiredService<BilibiliApi>(), ctx.GetRequiredService<IFileSystem>());
            });
        }
    }
}
