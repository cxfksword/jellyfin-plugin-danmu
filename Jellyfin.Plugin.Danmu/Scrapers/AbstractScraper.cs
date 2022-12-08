using System.Threading.Tasks;
using Jellyfin.Plugin.Danmu.Scrapers.Entity;
using MediaBrowser.Controller.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Danmu.Scrapers;

public abstract class AbstractScraper
{
    protected ILogger log;

    public virtual int DefaultOrder => 999;

    public virtual bool DefaultEnable => false;

    public abstract string Name { get; }

    /// <summary>
    /// Gets the provider name.
    /// </summary>
    public abstract string ProviderName { get; }

    /// <summary>
    /// Gets the provider id.
    /// </summary>
    public abstract string ProviderId { get; }


    public AbstractScraper(ILogger log)
    {
        this.log = log;
    }

    public abstract Task<string?> GetMatchMediaId(BaseItem item);

    public abstract Task<ScraperMedia?> GetMedia(string id);

    public abstract Task<ScraperEpisode?> GetMediaEpisode(string id);


    public abstract Task<ScraperDanmaku?> GetDanmuContent(string commentId);
}
