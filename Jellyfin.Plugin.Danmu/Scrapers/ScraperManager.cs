using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Jellyfin.Plugin.Danmu.Core.Extensions;
using MediaBrowser.Common;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Danmu.Scrapers;

public class ScraperManager
{
    protected ILogger log;
    private List<AbstractScraper> _scrapers = new List<AbstractScraper>();

    public ScraperManager(ILoggerFactory logManager)
    {
        log = logManager.CreateLogger<ScraperManager>();
        if (Plugin.Instance?.Scrapers != null)
        {
            this._scrapers.AddRange(Plugin.Instance.Scrapers);
        }
    }

    public void register(AbstractScraper scraper)
    {
        this._scrapers.Add(scraper);
    }

    public ReadOnlyCollection<AbstractScraper> All()
    {
        // 存在配置时，根据配置调整源顺序，并删除不启用的源
        if (Plugin.Instance?.Configuration.Scrapers != null)
        {
            var orderScrapers = new List<AbstractScraper>();

            var scraperMap = this._scrapers.ToDictionary(x => x.Name, x => x);
            var configScrapers = Plugin.Instance.Configuration.Scrapers;
            foreach (var config in configScrapers)
            {
                if (scraperMap.ContainsKey(config.Name) && config.Enable)
                {
                    orderScrapers.Add(scraperMap[config.Name]);
                }
            }

            // 添加新增并默认启用的源
            var allOldScaperNames = configScrapers.Select(o => o.Name).ToList();
            foreach (var scraper in this._scrapers)
            {
                if (!allOldScaperNames.Contains(scraper.Name) && scraper.DefaultEnable)
                {
                    orderScrapers.Add(scraper);
                }
            }

            return orderScrapers.AsReadOnly();
        }

        return this._scrapers.AsReadOnly();
    }
}
