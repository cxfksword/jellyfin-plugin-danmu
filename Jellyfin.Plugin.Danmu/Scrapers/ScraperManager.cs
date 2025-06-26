using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Jellyfin.Plugin.Danmu.Core.Extensions;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Danmu.Scrapers;

public class ScraperManager
{
    protected ILogger log;
    private List<AbstractScraper> _scrapers = new List<AbstractScraper>();

    public ScraperManager(ILoggerFactory logManager)
    {
        log = logManager.CreateLogger<ScraperManager>();
    }

    public void Register(AbstractScraper scraper)
    {
        this._scrapers.Add(scraper);
    }

    public void Register(IList<AbstractScraper> scrapers)
    {
        this._scrapers.AddRange(scrapers);
    }

    /// <summary>
    /// 更新所有scraper配置
    /// </summary>
    public void UpdateConfiguration(Configuration.ScraperConfigItem[] configItems)
    {
        foreach (var config in configItems)
        {
            var scraper = this._scrapers.FirstOrDefault(s => s.Name == config.Name);
            if (scraper != null)
            {
                scraper.InitializeWithConfig(config);
                log.LogInformation("Updated configuration for scraper {ScraperName}: {ConfigItem}", scraper.Name, config.ToJson());
            }
            else
            {
                log.LogWarning("Scraper {ScraperName} not found for configuration update.", config.Name);
            }
        }
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
