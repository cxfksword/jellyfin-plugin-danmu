using System;
using System.Collections.Generic;
using Jellyfin.Plugin.Danmu.Core;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.Danmu.Scrapers.Dandan.ExternalId;

/// <summary>
/// External URLs for Danmu.
/// </summary>
public class ExternalUrlProvider : IExternalUrlProvider
{
    /// <inheritdoc/>
    public string Name => Dandan.ScraperProviderName;

    /// <inheritdoc/>
    public IEnumerable<string> GetExternalUrls(BaseItem item)
    {
        switch (item)
        {
            case Season season:
                if (DanmuProviderId.TryGet(item, Dandan.ScraperProviderId, out var externalId))
                {
                    yield return $"https://api.dandanplay.net/api/v2/bangumi/{externalId}";
                }

                break;
            case Episode episode:
                if (DanmuProviderId.TryGet(item, Dandan.ScraperProviderId, out externalId))
                {
                    yield return "#";
                }

                break;
            case Movie:
                if (DanmuProviderId.TryGet(item, Dandan.ScraperProviderId, out externalId))
                {
                    yield return $"https://api.dandanplay.net/api/v2/bangumi/{externalId}";
                }

                break;
        }
    }
}