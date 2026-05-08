using System;
using System.Web;
using System.Collections.Generic;
using Jellyfin.Plugin.Danmu.Core;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.Danmu.Scrapers.Youku.ExternalId;

/// <summary>
/// External URLs for Danmu.
/// </summary>
public class ExternalUrlProvider : IExternalUrlProvider
{
    /// <inheritdoc/>
    public string Name => Youku.ScraperProviderName;

    /// <inheritdoc/>
    public IEnumerable<string> GetExternalUrls(BaseItem item)
    {
        switch (item)
        {
            case Season season:
                if (DanmuProviderId.TryGet(item, Youku.ScraperProviderId, out var externalId))
                {
                    yield return $"https://v.youku.com/v_nextstage/id_{externalId}.html";
                }

                break;
            case Episode episode:
                if (DanmuProviderId.TryGet(item, Youku.ScraperProviderId, out externalId))
                {
                    var decodeExternalId = HttpUtility.UrlDecode(externalId).Replace("||", "==");
                    yield return $"https://v.youku.com/v_show/id_{decodeExternalId}.html";
                }

                break;
            case Movie:
                if (DanmuProviderId.TryGet(item, Youku.ScraperProviderId, out externalId))
                {
                    yield return $"https://v.youku.com/v_nextstage/id_{externalId}.html";
                }

                break;
        }
    }
}