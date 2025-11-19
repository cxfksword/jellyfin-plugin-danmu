using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Web;
using Microsoft.Extensions.Caching.Memory;
using Jellyfin.Plugin.Danmu.Scrapers.Dandan.Entity;
using Jellyfin.Plugin.Danmu.Configuration;
using Jellyfin.Plugin.Danmu.Core.Extensions;
using MediaBrowser.Controller.Entities;
using System.IO;

namespace Jellyfin.Plugin.Danmu.Scrapers.Dandan;

public class DandanApi : AbstractApi
{
    const string API_ID = "";
    const string API_SECRET = "";
    private static readonly object _lock = new object();
    private DateTime lastRequestTime = DateTime.Now.AddDays(-1);

    public DandanOption Config
    {
        get
        {
            return Plugin.Instance?.Configuration.Dandan ?? new DandanOption();
        }
    }

    public string ApiID
    {
        get
        {
            var apiId = Environment.GetEnvironmentVariable("DANDAN_API_ID");
            if (!string.IsNullOrEmpty(apiId))
            {
                return apiId;
            }

            return API_ID;
        }
    }

    public string ApiSecret
    {
        get
        {
            var apiSecret = Environment.GetEnvironmentVariable("DANDAN_API_SECRET");
            if (!string.IsNullOrEmpty(apiSecret))
            {
                return apiSecret;
            }

            return API_SECRET;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DandanApi"/> class.
    /// </summary>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    public DandanApi(ILoggerFactory loggerFactory)
        : base(loggerFactory.CreateLogger<DandanApi>())
    {
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        httpClient.Timeout = TimeSpan.FromSeconds(10);
    }


    public async Task<List<Anime>> SearchAsync(string keyword, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(keyword))
        {
            return new List<Anime>();
        }

        var cacheKey = $"search_{keyword}";
        var expiredOption = new MemoryCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) };
        if (_memoryCache.TryGetValue<List<Anime>>(cacheKey, out var searchResult))
        {
            return searchResult;
        }

        this.LimitRequestFrequently();

        keyword = HttpUtility.UrlEncode(keyword);
        var url = $"https://api.dandanplay.net/api/v2/search/anime?keyword={keyword}";
        using var response = await this.Request(url, cancellationToken).ConfigureAwait(false);
        var result = await response.Content.ReadFromJsonAsync<SearchResult>(_jsonOptions, cancellationToken).ConfigureAwait(false);
        if (result != null && result.Success)
        {
            _memoryCache.Set<List<Anime>>(cacheKey, result.Animes, expiredOption);
            return result.Animes;
        }

        _memoryCache.Set<List<Anime>>(cacheKey, new List<Anime>(), expiredOption);
        return new List<Anime>();
    }

    public async Task<List<MatchResultV2>> MatchAsync(BaseItem item, CancellationToken cancellationToken)
    {
        if (item == null)
        {
            return new List<MatchResultV2>();
        }

        var cacheKey = $"match_{item.Id}";
        var expiredOption = new MemoryCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) };
        if (_memoryCache.TryGetValue<List<MatchResultV2>>(cacheKey, out var matches))
        {
            return matches;
        }

        var matchRequest = new Dictionary<string, object>
        {
            ["fileName"] = Path.GetFileNameWithoutExtension(item.Path),
            ["fileHash"] = "00000000000000000000000000000000",
            ["fileSize"] = item.Size ?? 0,
            ["videoDuration"] = (item.RunTimeTicks ?? 0) / 10000000,
            ["matchMode"] = "fileNameOnly",
        };
        if (this.Config.MatchByFileHash)
        {
            matchRequest["fileHash"] = await this.ComputeFileHashAsync(item.Path).ConfigureAwait(false);
            matchRequest["matchMode"] = "hashAndFileName";
        }

        var url = "https://api.dandanplay.net/api/v2/match";
        using var response = await this.Request(url, HttpMethod.Post, matchRequest, cancellationToken).ConfigureAwait(false);
        var result = await response.Content.ReadFromJsonAsync<MatchResponseV2>(_jsonOptions, cancellationToken).ConfigureAwait(false);
        if (result != null && result.Success && result.Matches != null)
        {
            _memoryCache.Set<List<MatchResultV2>>(cacheKey, result.Matches, expiredOption);
            return result.Matches;
        }

        _memoryCache.Set<List<MatchResultV2>>(cacheKey, new List<MatchResultV2>(), expiredOption);
        return new List<MatchResultV2>();
    }

    private async Task<string> ComputeFileHashAsync(string filePath)
    {
        try
        {
            using (var stream = File.OpenRead(filePath))
            {
                // 读取前16MB
                var buffer = new byte[16 * 1024 * 1024];
                var bytesRead = await stream.ReadAsync(buffer).ConfigureAwait(false);

                if (bytesRead > 0)
                {
                    // 如果文件小于16MB，调整buffer大小
                    if (bytesRead < buffer.Length)
                    {
                        Array.Resize(ref buffer, bytesRead);
                    }

                    var hash = MD5.HashData(buffer);
                    return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "计算文件哈希值时出错: {Path}", filePath);
        }

        return string.Empty;
    }

    public async Task<Anime?> GetAnimeAsync(long animeId, CancellationToken cancellationToken)
    {
        if (animeId <= 0)
        {
            return null;
        }

        var cacheKey = $"anime_{animeId}";
        var expiredOption = new MemoryCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) };
        if (_memoryCache.TryGetValue<Anime?>(cacheKey, out var anime))
        {
            return anime;
        }

        var url = $"https://api.dandanplay.net/api/v2/bangumi/{animeId}";
        using var response = await this.Request(url, cancellationToken).ConfigureAwait(false);

        var result = await response.Content.ReadFromJsonAsync<AnimeResult>(_jsonOptions, cancellationToken).ConfigureAwait(false);
        if (result != null && result.Success && result.Bangumi != null)
        {
            anime = result.Bangumi;
            if (anime.Episodes != null)
            {
                // 过滤掉特典剧集，episodeNumber为S1/S2.。。
                anime.Episodes = anime.Episodes.Where(x => x.EpisodeNumber.ToInt() > 0).ToList();

                // 本接口与 search 返回不完全一致，补全缺失字段
                anime.EpisodeCount = anime.Episodes.Count;
                anime.StartDate = anime.Episodes.FirstOrDefault()?.AirDate;
            }
            _memoryCache.Set<Anime?>(cacheKey, anime, expiredOption);
            return anime;
        }

        _memoryCache.Set<Anime?>(cacheKey, null, expiredOption);
        return null;
    }

    public async Task<List<Comment>> GetCommentsAsync(long epId, CancellationToken cancellationToken)
    {
        if (epId <= 0)
        {
            throw new ArgumentNullException(nameof(epId));
        }

        this.LimitRequestFrequently();

        var withRelated = this.Config.WithRelatedDanmu ? "true" : "false";
        var chConvert = this.Config.ChConvert;
        var url = $"https://api.dandanplay.net/api/v2/comment/{epId}?withRelated={withRelated}&chConvert={chConvert}";
        using var response = await this.Request(url, cancellationToken).ConfigureAwait(false);
        var result = await response.Content.ReadFromJsonAsync<CommentResult>(_jsonOptions, cancellationToken).ConfigureAwait(false);
        if (result != null)
        {
            return result.Comments;
        }

        throw new Exception($"Request fail. epId={epId}");
    }

    protected void LimitRequestFrequently(double intervalMilliseconds = 1000)
    {
        var diff = 0;
        lock (_lock)
        {
            var ts = DateTime.Now - lastRequestTime;
            diff = (int)(intervalMilliseconds - ts.TotalMilliseconds);
            lastRequestTime = DateTime.Now;
        }

        if (diff > 0)
        {
            this._logger.LogDebug("请求太频繁，等待{0}毫秒后继续执行...", diff);
            Thread.Sleep(diff);
        }
    }

    protected async Task<HttpResponseMessage> Request(string url, CancellationToken cancellationToken)
    {
        return await Request(url, HttpMethod.Get, null, cancellationToken).ConfigureAwait(false);
    }

    protected async Task<HttpResponseMessage> Request(string url, HttpMethod method, object? content = null, CancellationToken cancellationToken = default)
    {
        var timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
        var signature = GenerateSignature(url, timestamp);

        HttpResponseMessage response;
        using (var request = new HttpRequestMessage(method, url)) {
            request.Headers.Add("X-AppId", ApiID);
            request.Headers.Add("X-Signature", signature);
            request.Headers.Add("X-Timestamp", timestamp.ToString());
            if (method == HttpMethod.Post && content != null)
            {
                request.Content = JsonContent.Create(content, null, _jsonOptions);
            }
            response = await this.httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }

        return response;
    }

    protected string GenerateSignature(string url, long timestamp)
    {
        if (string.IsNullOrEmpty(ApiID) || string.IsNullOrEmpty(ApiSecret))
        {
            throw new Exception("弹弹接口缺少API_ID和API_SECRET");
        }
        var uri = new Uri(url);
        var path = uri.AbsolutePath;
        var str = $"{ApiID}{timestamp}{path}{ApiSecret}";
        using (var sha256 = SHA256.Create())
        {
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(str));
            return Convert.ToBase64String(hashBytes);
        }
    }

}
