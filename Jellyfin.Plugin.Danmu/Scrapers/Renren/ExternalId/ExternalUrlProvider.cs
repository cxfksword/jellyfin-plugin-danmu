using System;
using System.Collections.Generic;
using Jellyfin.Plugin.Danmu.Core;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.Danmu.Scrapers.Renren.ExternalId;

public class ExternalUrlProvider : IExternalUrlProvider
{
    public string Name => Renren.ScraperProviderName;

    public IEnumerable<string> GetExternalUrls(BaseItem item)
    {
        switch (item)
        {
            case Season season:
                if (DanmuProviderId.TryGet(item, Renren.ScraperProviderId, out var externalId))
                {
                    yield return $"https://mh.yichengwlkj.com/pc/drama/{externalId}";
                }

                break;
            case Episode episode:
                if (DanmuProviderId.TryGet(item, Renren.ScraperProviderId, out externalId))
                {
                    if (episode.Season != null && DanmuProviderId.TryGet(episode.Season, Renren.ScraperProviderId, out var seasonExternalId))
                    {
                        yield return $"https://mh.yichengwlkj.com/pc/drama/{seasonExternalId}?episodeNo={episode.IndexNumber}";
                    }
                    else
                    {
                        yield return "#";
                    }
                }

                break;
            case Movie:
                if (DanmuProviderId.TryGet(item, Renren.ScraperProviderId, out externalId))
                {
                    yield return $"https://mh.yichengwlkj.com/pc/drama/{externalId}";
                }

                break;
        }
    }
}
