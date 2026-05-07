using System;
using System.Collections.Generic;
using Jellyfin.Plugin.Danmu.Core;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.Danmu.Scrapers.Tencent.ExternalId;

/// <summary>
/// External URLs for Danmu.
/// </summary>
public class ExternalUrlProvider : IExternalUrlProvider
{
    /// <inheritdoc/>
    public string Name => Tencent.ScraperProviderName;

    /// <inheritdoc/>
    public IEnumerable<string> GetExternalUrls(BaseItem item)
    {
        switch (item)
        {
            case Season season:
                if (DanmuProviderId.TryGet(item, Tencent.ScraperProviderId, out var externalId))
                {
                    yield return $"https://v.qq.com/x/cover/{externalId}.html";
                }

                break;
            case Episode episode:
                if (DanmuProviderId.TryGet(item, Tencent.ScraperProviderId, out externalId))
                {
                    if (episode.Season != null && DanmuProviderId.TryGet(episode.Season, Tencent.ScraperProviderId, out var seasonExternalId))
                    {
                        yield return $"https://v.qq.com/x/cover/{seasonExternalId}/{externalId}.html";
                    }
                    else
                    {
                        yield return "#";
                    }
                }

                break;
            case Movie:
                if (DanmuProviderId.TryGet(item, Tencent.ScraperProviderId, out externalId))
                {
                    yield return $"https://v.qq.com/x/cover/{externalId}.html";
                }

                break;
        }
    }
}