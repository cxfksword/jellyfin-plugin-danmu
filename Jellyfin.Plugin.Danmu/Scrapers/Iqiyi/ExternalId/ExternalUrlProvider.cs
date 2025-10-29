using System;
using System.Collections.Generic;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.Danmu.Scrapers.Iqiyi.ExternalId;

/// <summary>
/// External URLs for Danmu.
/// </summary>
public class ExternalUrlProvider : IExternalUrlProvider
{
    /// <inheritdoc/>
    public string Name => Iqiyi.ScraperProviderName;

    /// <inheritdoc/>
    public IEnumerable<string> GetExternalUrls(BaseItem item)
    {
        switch (item)
        {
            case Season season:
                if (item.TryGetProviderId(Iqiyi.ScraperProviderId, out var externalId))
                {
                    yield return $"https://www.iqiyi.com/v_{externalId}.html";
                }

                break;
            case Episode episode:
                if (item.TryGetProviderId(Iqiyi.ScraperProviderId, out externalId))
                {
                    yield return $"https://www.iqiyi.com/v_{externalId}.html";
                }

                break;
            case Movie:
                if (item.TryGetProviderId(Iqiyi.ScraperProviderId, out externalId))
                {
                    yield return $"https://www.iqiyi.com/v_{externalId}.html";
                }

                break;
        }
    }
}