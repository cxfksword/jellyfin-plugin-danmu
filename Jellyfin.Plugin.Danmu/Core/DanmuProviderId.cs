using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Plugin.Danmu.Scrapers;
using Jellyfin.Plugin.Danmu.Scrapers.Bilibili;
using Jellyfin.Plugin.Danmu.Scrapers.Dandan;
using Jellyfin.Plugin.Danmu.Scrapers.DanmuApi;
using Jellyfin.Plugin.Danmu.Scrapers.Iqiyi;
using Jellyfin.Plugin.Danmu.Scrapers.Mgtv;
using Jellyfin.Plugin.Danmu.Scrapers.Tencent;
using Jellyfin.Plugin.Danmu.Scrapers.Youku;
using Jellyfin.Plugin.Danmu.Scrapers.Renren;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.Danmu.Core;

public static class DanmuProviderId
{
    public const string UnifiedProviderId = "DanmuID";
    public const string UnifiedProviderName = "Danmu";

    private const char Separator = ':';

    private static readonly IReadOnlyDictionary<string, string> ScraperProviderIdToPrefixMap = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [Bilibili.ScraperProviderId] = "bilibili",
        [Dandan.ScraperProviderId] = "dandan",
        [DanmuApi.ScraperProviderId] = "danmuapi",
        [Iqiyi.ScraperProviderId] = "iqiyi",
        [Mgtv.ScraperProviderId] = "mgtv",
        [Tencent.ScraperProviderId] = "tencent",
        [Youku.ScraperProviderId] = "youku",
        [Renren.ScraperProviderId] = "renren",
    };

    private static readonly IReadOnlyDictionary<string, string> PrefixToScraperProviderIdMap = ScraperProviderIdToPrefixMap
        .ToDictionary(pair => pair.Value, pair => pair.Key, StringComparer.OrdinalIgnoreCase);

    public static string Encode(string scraperProviderId, string providerValue)
    {
        return $"{ToPrefix(scraperProviderId)}{Separator}{providerValue}";
    }

    public static bool TryDecode(string? value, out string scraperProviderId, out string providerValue)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            scraperProviderId = string.Empty;
            providerValue = string.Empty;
            return false;
        }

        var separatorIndex = value.IndexOf(Separator);
        if (separatorIndex <= 0 || separatorIndex >= value.Length - 1)
        {
            scraperProviderId = string.Empty;
            providerValue = string.Empty;
            return false;
        }

        scraperProviderId = ToScraperProviderId(value[..separatorIndex]);
        providerValue = value[(separatorIndex + 1)..];
        return true;
    }

    public static string Get(IHasProviderIds item, string scraperProviderId)
    {
        return TryGet(item, scraperProviderId, out var providerValue) ? providerValue : string.Empty;
    }

    public static bool TryGet(IHasProviderIds item, string scraperProviderId, out string providerValue)
    {
        if (item.ProviderIds.ContainsKey(UnifiedProviderId))
        {
            if (TryGetUnified(item, out var matchedProviderId, out var unifiedProviderValue)
                && string.Equals(matchedProviderId, scraperProviderId, StringComparison.Ordinal))
            {
                providerValue = unifiedProviderValue;
                return true;
            }

            providerValue = string.Empty;
            return false;
        }

        if (item.ProviderIds.TryGetValue(scraperProviderId, out var legacyProviderValue)
            && !string.IsNullOrEmpty(legacyProviderValue))
        {
            providerValue = legacyProviderValue;
            return true;
        }

        providerValue = string.Empty;
        return false;
    }

    public static bool TryGetFirst(IHasProviderIds item, IEnumerable<AbstractScraper> scrapers, out AbstractScraper? scraper, out string providerValue)
    {
        if (item.ProviderIds.ContainsKey(UnifiedProviderId))
        {
            if (TryGetUnified(item, out var unifiedScraperProviderId, out var unifiedProviderValue))
            {
                scraper = scrapers.FirstOrDefault(current => string.Equals(current.ProviderId, unifiedScraperProviderId, StringComparison.Ordinal));
                if (scraper != null)
                {
                    providerValue = unifiedProviderValue;
                    return true;
                }
            }

            scraper = null;
            providerValue = string.Empty;
            return false;
        }

        foreach (var currentScraper in scrapers)
        {
            if (item.ProviderIds.TryGetValue(currentScraper.ProviderId, out var legacyProviderValue)
                && !string.IsNullOrEmpty(legacyProviderValue))
            {
                scraper = currentScraper;
                providerValue = legacyProviderValue;
                return true;
            }
        }

        scraper = null;
        providerValue = string.Empty;
        return false;
    }

    public static bool HasAny(IHasProviderIds item, IEnumerable<AbstractScraper> scrapers)
    {
        return TryGetFirst(item, scrapers, out _, out _);
    }

    public static bool TryGetUnified(IHasProviderIds item, out string scraperProviderId, out string providerValue)
    {
        if (item.ProviderIds.TryGetValue(UnifiedProviderId, out var rawProviderValue)
            && TryDecode(rawProviderValue, out scraperProviderId, out providerValue))
        {
            return true;
        }

        scraperProviderId = string.Empty;
        providerValue = string.Empty;
        return false;
    }

    public static void Clear(BaseItem item, IEnumerable<AbstractScraper> scrapers)
    {
        item.ProviderIds.Remove(UnifiedProviderId);
        foreach (var scraper in scrapers)
        {
            item.ProviderIds.Remove(scraper.ProviderId);
        }
    }

    public static void Set(BaseItem item, IEnumerable<AbstractScraper> scrapers, string scraperProviderId, string providerValue)
    {
        Clear(item, scrapers);
        item.SetProviderId(UnifiedProviderId, Encode(scraperProviderId, providerValue));
    }

    public static bool TryMigrateToUnified(BaseItem item, IEnumerable<AbstractScraper> scrapers, string scraperProviderId, string providerValue)
    {
        if (string.IsNullOrEmpty(providerValue))
        {
            return false;
        }

        var scraperList = scrapers as IReadOnlyCollection<AbstractScraper> ?? scrapers.ToArray();
        var encodedProviderValue = Encode(scraperProviderId, providerValue);
        var hasCanonicalUnifiedProviderId = item.ProviderIds.TryGetValue(UnifiedProviderId, out var rawProviderValue)
            && string.Equals(rawProviderValue, encodedProviderValue, StringComparison.Ordinal);
        var hasLegacyScraperProviderId = scraperList.Any(scraper => item.ProviderIds.ContainsKey(scraper.ProviderId));

        if (hasCanonicalUnifiedProviderId && !hasLegacyScraperProviderId)
        {
            return false;
        }

        Set(item, scraperList, scraperProviderId, providerValue);
        return true;
    }

    private static string ToPrefix(string scraperProviderId)
    {
        return ScraperProviderIdToPrefixMap.TryGetValue(scraperProviderId, out var prefix)
            ? prefix
            : scraperProviderId;
    }

    private static string ToScraperProviderId(string prefix)
    {
        return PrefixToScraperProviderIdMap.TryGetValue(prefix, out var scraperProviderId)
            ? scraperProviderId
            : prefix;
    }
}