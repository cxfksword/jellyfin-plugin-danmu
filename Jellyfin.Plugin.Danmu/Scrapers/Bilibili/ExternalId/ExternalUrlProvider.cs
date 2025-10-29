using System;
using System.Collections.Generic;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.Danmu.Scrapers.Bilibili.ExternalId;

/// <summary>
/// External URLs for Danmu.
/// </summary>
public class ExternalUrlProvider : IExternalUrlProvider
{
    /// <inheritdoc/>
    public string Name => Bilibili.ScraperProviderName;

    /// <inheritdoc/>
    public IEnumerable<string> GetExternalUrls(BaseItem item)
    {
        switch (item)
        {
            case Season season:
                if (item.TryGetProviderId(Bilibili.ScraperProviderId, out var externalId))
                {
                    if (externalId.StartsWith("bv", StringComparison.OrdinalIgnoreCase) || externalId.StartsWith("av", StringComparison.OrdinalIgnoreCase))
                    {
                        yield return $"https://www.bilibili.com/{externalId}";
                    }
                    else
                    {
                        yield return $"https://www.bilibili.com/bangumi/play/ss{externalId}";
                    }
                }

                break;
            case Episode episode:
                if (item.TryGetProviderId(Bilibili.ScraperProviderId, out externalId))
                {
                    yield return $"https://www.bilibili.com/bangumi/play/ep{externalId}";
                }

                break;
            case Movie:
                if (item.TryGetProviderId(Bilibili.ScraperProviderId, out externalId))
                {
                    yield return $"https://www.bilibili.com/bangumi/play/ep{externalId}";
                }

                break;
        }
    }
}