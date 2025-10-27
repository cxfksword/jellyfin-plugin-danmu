using System;
using System.Collections.Generic;
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
                if (item.TryGetProviderId(Dandan.ScraperProviderId, out var externalId))
                {
                    yield return $"https://api.dandanplay.net/api/v2/bangumi/{externalId}";
                }

                break;
            case Episode episode:
                if (item.TryGetProviderId(Dandan.ScraperProviderId, out externalId))
                {
                    yield return "#";
                }

                break;
            case Movie:
                if (item.TryGetProviderId(Dandan.ScraperProviderId, out externalId))
                {
                    yield return $"https://api.dandanplay.net/api/v2/bangumi/{externalId}";
                }

                break;
        }
    }
}