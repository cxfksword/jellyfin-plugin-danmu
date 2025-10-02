using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Jellyfin.Plugin.Danmu.Configuration;
using Jellyfin.Plugin.Danmu.Scrapers;
using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.Danmu;

/// <summary>
/// The main plugin.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Plugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
    /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
    public Plugin(IApplicationPaths applicationPaths, IApplicationHost applicationHost, IXmlSerializer xmlSerializer, ScraperManager scraperManager)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
        Scrapers = applicationHost.GetExports<AbstractScraper>(false).Where(o => o != null && !o.IsDeprecated).OrderBy(x => x.DefaultOrder).ToList().AsReadOnly();
        scraperManager.Register(Scrapers);
    }

    /// <inheritdoc />
    public override string Name => "Danmu";

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("5B39DA44-5314-4940-8E26-54C821C17F86");

    /// <summary>
    /// Gets the current plugin instance.
    /// </summary>
    public static Plugin? Instance { get; private set; }

    /// <summary>
    /// 全部的弹幕源
    /// </summary>
    public ReadOnlyCollection<AbstractScraper> Scrapers { get; }

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        return new[]
        {
            new PluginPageInfo
            {
                Name = this.Name,
                EmbeddedResourcePath = string.Format(CultureInfo.InvariantCulture, "{0}.Configuration.configPage.html", GetType().Namespace)
            }
        };
    }
}
