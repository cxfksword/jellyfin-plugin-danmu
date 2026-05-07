using System;
using System.Collections.Generic;
using Jellyfin.Plugin.Danmu.Core;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.Danmu.Scrapers.Mgtv.ExternalId;

/// <summary>
/// External URLs for Danmu.
/// </summary>
public class ExternalUrlProvider : IExternalUrlProvider
{
    /// <inheritdoc/>
    public string Name => Mgtv.ScraperProviderName;

    /// <inheritdoc/>
    public IEnumerable<string> GetExternalUrls(BaseItem item)
    {
        switch (item)
        {
            case Season season:
                if (DanmuProviderId.TryGet(item, Mgtv.ScraperProviderId, out var externalId))
                {
                    yield return $"https://www.mgtv.com/h/{externalId}.html";
                }

                break;
            case Episode episode:
                if (DanmuProviderId.TryGet(item, Mgtv.ScraperProviderId, out externalId))
                {
                    if (episode.Season != null && DanmuProviderId.TryGet(episode.Season, Mgtv.ScraperProviderId, out var seasonExternalId))
                    {
                        yield return $"https://www.mgtv.com/b/{seasonExternalId}/{externalId}.html";
                    }
                    else
                    {
                        yield return "#";
                    }
                }

                break;
            case Movie:
                if (DanmuProviderId.TryGet(item, Mgtv.ScraperProviderId, out externalId))
                {
                    yield return $"https://www.mgtv.com/h/{externalId}.html";
                }

                break;
        }
    }
}