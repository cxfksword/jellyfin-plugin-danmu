using System.Collections.Generic;
using System.Collections.ObjectModel;
using Jellyfin.Plugin.Danmu.Api;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Danmu.Scrapers;

public class ScraperFactory
{
    private List<AbstractScraper> scrapers { get; }

    public ScraperFactory(ILoggerFactory logManager, BilibiliApi api)
    {
        scrapers = new List<AbstractScraper>() {
            new Jellyfin.Plugin.Danmu.Scrapers.Bilibili.Bilibili(logManager, api)
        };
    }

    public ReadOnlyCollection<AbstractScraper> All()
    {
        return new ReadOnlyCollection<AbstractScraper>(scrapers);
    }
}
