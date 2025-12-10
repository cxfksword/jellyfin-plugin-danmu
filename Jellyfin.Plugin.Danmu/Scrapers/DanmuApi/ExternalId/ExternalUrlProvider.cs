using System;
using System.Collections.Generic;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.Danmu.Scrapers.DanmuApi.ExternalId;

/// <summary>
/// External URLs for DanmuApi.
/// </summary>
public class ExternalUrlProvider : IExternalUrlProvider
{
    /// <inheritdoc/>
    public string Name => DanmuApi.ScraperProviderName;

    /// <inheritdoc/>
    public IEnumerable<string> GetExternalUrls(BaseItem item)
    {
        switch (item)
        {
            case Season season:
                if (item.TryGetProviderId(DanmuApi.ScraperProviderId, out var externalId))
                {
                    var serverUrl = GetServerUrl();
                    if (!string.IsNullOrEmpty(serverUrl))
                    {
                        yield return $"{serverUrl}/api/v2/bangumi/{externalId}";
                    }
                    else
                    {
                        yield return "#";
                    }
                }

                break;
            case Episode episode:
                if (item.TryGetProviderId(DanmuApi.ScraperProviderId, out externalId))
                {
                    var serverUrl = GetServerUrl();
                    if (!string.IsNullOrEmpty(serverUrl))
                    {
                        yield return $"{serverUrl}/api/v2/comment/{externalId}";
                    }
                    else
                    {
                        yield return "#";
                    }
                }

                break;
            case Movie:
                if (item.TryGetProviderId(DanmuApi.ScraperProviderId, out externalId))
                {
                    var serverUrl = GetServerUrl();
                    if (!string.IsNullOrEmpty(serverUrl))
                    {
                        yield return $"{serverUrl}/api/v2/bangumi/{externalId}";
                    }
                }

                break;
        }
    }

    private string GetServerUrl()
    {
        var serverUrl = Plugin.Instance?.Configuration?.DanmuApi?.ServerUrl?.Trim();
        if (string.IsNullOrEmpty(serverUrl))
        {
            // 尝试从环境变量获取
            serverUrl = Environment.GetEnvironmentVariable("DANMU_API_SERVER_URL");
            if (string.IsNullOrEmpty(serverUrl))
            {
                return string.Empty;
            }
        }

        // 移除末尾的 /
        return serverUrl.TrimEnd('/');
    }
}
