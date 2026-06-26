using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ComposableAsync;
using Jellyfin.Plugin.Danmu.Scrapers.Renren.Entity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using RateLimiter;

namespace Jellyfin.Plugin.Danmu.Scrapers.Renren;

public class RenrenApi : AbstractApi
{
    private const string TvHost = "api.gorafie.com";
    private const string TvDanmuHost = "static-dm.qwdjapp.com";
    private const string TvVersion = "1.2.2";
    private const string TvUserAgent = "okhttp/3.12.13";
    private const string TvClientType = "android_qwtv_RRSP";
    private const string TvPkt = "rrmj";
    private const string TvSecretKey = "cf65GPholnICgyw1xbrpA79XVkizOdMq";
    private const string DeviceId = "tWEtIN7JG2DTDkBBigvj6A%3D%3D";

    private readonly Random _random = new();
    private TimeLimiter _timeConstraint = TimeLimiter.GetFromMaxCountByInterval(5, TimeSpan.FromSeconds(1));
    private TimeLimiter _danmuTimeConstraint = TimeLimiter.GetFromMaxCountByInterval(1, TimeSpan.FromSeconds(5));

    public RenrenApi(ILoggerFactory loggerFactory)
        : base(loggerFactory.CreateLogger<RenrenApi>())
    {
        httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<List<SearchItem>> SearchAsync(string keyword, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(keyword))
        {
            return new List<SearchItem>();
        }

        var cacheKey = $"renren_search_{keyword}";
        var expiredOption = new MemoryCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) };
        if (_memoryCache.TryGetValue<List<SearchItem>>(cacheKey, out var cacheValue))
        {
            return cacheValue;
        }

        await _timeConstraint;

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var path = "/qwtv/search";
        var queryParams = new Dictionary<string, string>
        {
            ["searchWord"] = keyword,
            ["num"] = "30",
            ["searchNext"] = "",
            ["well"] = "match"
        };

        var sign = GenerateSign(path, timestamp, queryParams, TvSecretKey);
        var queryString = string.Join("&",
            queryParams.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value ?? "")}"));

        var url = $"https://{TvHost}{path}?{queryString}";

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            foreach (var header in BuildTvHeaders(timestamp, sign))
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<SearchResponse>(_jsonOptions, cancellationToken).ConfigureAwait(false);
            if (result != null && result.Code == "0000" && result.Data != null)
            {
                _memoryCache.Set(cacheKey, result.Data, expiredOption);
                return result.Data;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Renren] 搜索失败: {Keyword}", keyword);
        }

        var emptyList = new List<SearchItem>();
        _memoryCache.Set(cacheKey, emptyList, expiredOption);
        return emptyList;
    }

    public async Task<DetailData?> GetDetailAsync(string dramaId, string episodeSid, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(dramaId))
        {
            return null;
        }

        var cacheKey = $"renren_detail_{dramaId}_{episodeSid}";
        var expiredOption = new MemoryCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) };
        if (_memoryCache.TryGetValue<DetailData?>(cacheKey, out var cacheValue))
        {
            return cacheValue;
        }

        await _timeConstraint;

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var path = "/qwtv/drama/details";
        var queryParams = new Dictionary<string, string>
        {
            ["isAgeLimit"] = "false",
            ["seriesId"] = dramaId,
            ["episodeId"] = episodeSid ?? "",
            ["clarity"] = "HD",
            ["caption"] = "0",
            ["hevcOpen"] = "1"
        };

        var sign = GenerateSign(path, timestamp, queryParams, TvSecretKey);
        var queryString = string.Join("&",
            queryParams.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value ?? "")}"));

        var url = $"https://{TvHost}{path}?{queryString}";

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            foreach (var header in BuildTvHeaders(timestamp, sign))
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<DetailResponse>(_jsonOptions, cancellationToken).ConfigureAwait(false);
            if (result != null && result.Code == "0000" && result.Data != null)
            {
                _memoryCache.Set(cacheKey, result.Data, expiredOption);
                return result.Data;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Renren] 获取详情失败: {DramaId}", dramaId);
        }

        _memoryCache.Set<DetailData?>(cacheKey, null, expiredOption);
        return null;
    }

    public async Task<List<DanmuItem>> GetDanmuAsync(string episodeSid, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(episodeSid))
        {
            return new List<DanmuItem>();
        }

        await _danmuTimeConstraint;

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var path = $"/v1/produce/danmu/EPISODE/{episodeSid}";
        var queryParams = new Dictionary<string, string>();
        var sign = GenerateSign(path, timestamp, queryParams, TvSecretKey);

        var url = $"https://{TvDanmuHost}{path}";

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            foreach (var header in BuildTvHeaders(timestamp, sign))
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrEmpty(content))
            {
                return new List<DanmuItem>();
            }

            // Response can be a direct array or wrapped in {"data": [...]}
            try
            {
                var items = System.Text.Json.JsonSerializer.Deserialize<List<DanmuItem>>(content, _jsonOptions);
                if (items != null)
                {
                    return items;
                }
            }
            catch
            {
            }

            try
            {
                var danmuData = System.Text.Json.JsonSerializer.Deserialize<DanmuData>(content, _jsonOptions);
                if (danmuData?.Data != null)
                {
                    return danmuData.Data;
                }
            }
            catch
            {
            }

            return new List<DanmuItem>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Renren] 获取弹幕失败: {EpisodeSid}", episodeSid);
            return new List<DanmuItem>();
        }
    }

    private Dictionary<string, string> BuildTvHeaders(long timestamp, string sign)
    {
        var aliId = GenerateRandomAliId();
        return new Dictionary<string, string>
        {
            ["clientVersion"] = TvVersion,
            ["p"] = "Android",
            ["deviceid"] = DeviceId,
            ["token"] = "",
            ["aliid"] = aliId,
            ["umid"] = "",
            ["clienttype"] = TvClientType,
            ["pkt"] = TvPkt,
            ["t"] = timestamp.ToString(),
            ["sign"] = sign,
            ["isAgree"] = "1",
            ["et"] = "2",
            ["Accept-Encoding"] = "gzip",
            ["User-Agent"] = TvUserAgent,
        };
    }

    private static string GenerateSign(string path, long timestamp, Dictionary<string, string>? paramsDict, string secretKey)
    {
        var signStr = new StringBuilder();
        signStr.Append(path);
        signStr.Append('t');
        signStr.Append(timestamp);

        if (paramsDict != null && paramsDict.Count > 0)
        {
            foreach (var kv in paramsDict.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                signStr.Append(kv.Key);
                signStr.Append(kv.Value ?? "");
            }
        }

        signStr.Append(secretKey);

        return MD5Hash(signStr.ToString());
    }

    private static string MD5Hash(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = MD5.HashData(bytes);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }

    private string GenerateRandomAliId()
    {
        const string prefix = "aY";
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
        const int length = 22;
        var result = new char[prefix.Length + length];
        for (int i = 0; i < prefix.Length; i++)
            result[i] = prefix[i];
        for (int i = 0; i < length; i++)
        {
            result[prefix.Length + i] = chars[_random.Next(chars.Length)];
        }
        return new string(result);
    }
}
