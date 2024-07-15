using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Emby.Plugin.Danmu.Configuration;
using Emby.Plugin.Danmu.Core.Extensions;
using Emby.Plugin.Danmu.Core.Singleton;
using Emby.Plugin.Danmu.Scraper;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Emby.Plugin.Danmu
{
    using System;
    using System.IO;

    using MediaBrowser.Common;
    using MediaBrowser.Common.Plugins;
    using MediaBrowser.Model.Drawing;
    using MediaBrowser.Model.Logging;

    /// <summary>
    /// The plugin.
    /// </summary>
    public class Plugin : BasePlugin<PluginConfiguration>, IHasThumbImage, IHasWebPages
    {
        public static Plugin Instance { get; private set; }
        
        /// <summary>The Plugin ID.</summary>
        private readonly Guid id = new Guid("cdbc5624-3ea9-4f9d-94cc-3be20585f926"); // << Generate one: Tools >> Create GUID

        private readonly ILogger logger;

        /// <summary>Initializes a new instance of the <see cref="Plugin" /> class.</summary>
        /// <param name="applicationPaths">The application host.</param>
        /// <param name="applicationHost"></param>
        /// <param name="logManager">The log manager.</param>
        /// <param name="xmlSerializer"></param>
        /// <param name="scraperManager"></param>
        /// <param name="jsonSerializer"></param>
        /// <param name="httpClient"></param>
        public Plugin(IApplicationPaths applicationPaths, 
            IApplicationHost applicationHost, 
            ILogManager logManager, 
            IXmlSerializer xmlSerializer, 
            ScraperManager scraperManager, 
            IJsonSerializer jsonSerializer,
            IHttpClient httpClient
            ) 
            : base(applicationPaths, xmlSerializer)
        {
            SingletonManager.ScraperManager = scraperManager;
            SingletonManager.JsonSerializer = jsonSerializer;
            SingletonManager.HttpClient = httpClient;
            SingletonManager.LogManager = logManager;
            SingletonManager.applicationHost = applicationHost;
            SingletonManager.LibraryManagerEventsHelper = applicationHost.Resolve<LibraryManagerEventsHelper>();

            logger = logManager.getDefaultLogger();
            Instance = this;
            Scrapers = applicationHost.GetExports<AbstractScraper>(false)
                .Where(o => o != null)
                // .Where(o => Iqiyi.ScraperProviderId.Equals(o.ProviderId))
                .OrderBy(x => x.DefaultOrder)
                .ToList()
                .AsReadOnly();
            scraperManager.Register(Scrapers);
            logger.Info("danmu 插件加载完成, 支持{0}个, {1}", Scrapers.Count, Scrapers.ToJson());
        }

        public override string Description => "全网弹幕插件";

        public override Guid Id => this.id;
        public sealed override string Name => "Danmu";

        /// <summary>Gets the thumb image format.</summary>
        /// <value>The thumb image format.</value>
        public ImageFormat ThumbImageFormat => ImageFormat.Png;

        /// <summary>Gets the thumb image.</summary>
        /// <returns>An image stream.</returns>
        public Stream GetThumbImage()
        {
            var type = this.GetType();
            return type.Assembly.GetManifestResourceStream(type.Namespace + ".Configuration.logo.png");
        }
        
        /// <summary>
        /// 全部的弹幕源
        /// </summary>
        public ReadOnlyCollection<AbstractScraper> Scrapers { get; }
        
        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = "danmu",
                    DisplayName = "弹幕配置",
                    MenuIcon = "closed_caption",
                    MenuSection = "server",
                    EnableInMainMenu = true,
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.configPage.html",
                },
                new PluginPageInfo
                {
                    Name = "danmuJs",
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.config.js"
                }
            };
        }
    }
}
