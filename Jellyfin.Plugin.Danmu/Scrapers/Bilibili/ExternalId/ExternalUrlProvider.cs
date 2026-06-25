using System;
using System.Collections.Generic;
using Jellyfin.Plugin.Danmu.Core;
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
    private const ulong XorCode = 23442827791579;
    private const ulong MaxAid = 1UL << 51;
    private const ulong Base = 58;
    private static readonly char[] BvidAlphabet = "FcwAPNKTMug3GV5Lj7EJnHpWsx4tb8haYeviqBz6rkCy12mUSDQX9RdoZf".ToCharArray();

    /// <inheritdoc/>
    public string Name => Bilibili.ScraperProviderName;

    /// <inheritdoc/>
    public IEnumerable<string> GetExternalUrls(BaseItem item)
    {
        switch (item)
        {
            case Season season:
                if (DanmuProviderId.TryGet(item, Bilibili.ScraperProviderId, out var externalId))
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
                if (DanmuProviderId.TryGet(item, Bilibili.ScraperProviderId, out externalId))
                {
                    if (TryBuildVideoUrl(externalId, out var videoUrl))
                    {
                        yield return videoUrl;
                        yield break;
                    }

                    yield return $"https://www.bilibili.com/bangumi/play/ep{externalId}";
                }

                break;
            case Movie:
                if (DanmuProviderId.TryGet(item, Bilibili.ScraperProviderId, out externalId))
                {
                    if (externalId.StartsWith("bv", StringComparison.OrdinalIgnoreCase) || externalId.StartsWith("av", StringComparison.OrdinalIgnoreCase))
                    {
                        yield return $"https://www.bilibili.com/{externalId}";
                    }
                    else
                    {
                        yield return $"https://www.bilibili.com/bangumi/play/ep{externalId}";
                    }
                }

                break;
        }
    }

    private static bool TryBuildVideoUrl(string externalId, out string url)
    {
        url = string.Empty;

        var segments = externalId.Split(',', 2, StringSplitOptions.TrimEntries);
        if (segments.Length != 2)
        {
            return false;
        }

        var aidText = segments[0];
        if (!ulong.TryParse(aidText, out var aid) || aid == 0)
        {
            return false;
        }

        url = $"https://www.bilibili.com/video/{ConvertAidToBvid(aid)}";
        return true;
    }

    private static string ConvertAidToBvid(ulong aid)
    {
        var bytes = "BV1000000000".ToCharArray();
        var index = bytes.Length - 1;
        var value = (MaxAid | aid) ^ XorCode;

        while (value != 0)
        {
            bytes[index] = BvidAlphabet[(int)(value % Base)];
            value /= Base;
            index--;
        }

        (bytes[3], bytes[9]) = (bytes[9], bytes[3]);
        (bytes[4], bytes[7]) = (bytes[7], bytes[4]);
        return new string(bytes);
    }
}