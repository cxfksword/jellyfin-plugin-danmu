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

    /// <summary>
    /// Initializes a new instance of the <see cref="DandanApi"/> class.
    /// </summary>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    public DandanApi(ILoggerFactory loggerFactory)
        : base(loggerFactory.CreateLogger<DandanApi>())
    {
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
    }


    public async Task<List<Anime>> SearchAsync(string keyword, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(keyword))
        {
            return new List<Anime>();
        }

        var cacheKey = $"search_{keyword}";
        var expiredOption = new MemoryCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) };
        if (_memoryCache.TryGetValue<List<Anime>>(cacheKey, out var searchResult))
        {
            return searchResult;
        }

        this.LimitRequestFrequently();

        keyword = HttpUtility.UrlEncode(keyword);
        var url = $"https://api.dandanplay.net/api/v2/search/anime?keyword={keyword}";
        var response = await this.Request(url, cancellationToken).ConfigureAwait(false);
        var result = await response.Content.ReadFromJsonAsync<SearchResult>(_jsonOptions, cancellationToken).ConfigureAwait(false);
        if (result != null && result.Success)
        {
            _memoryCache.Set<List<Anime>>(cacheKey, result.Animes, expiredOption);
            return result.Animes;
        }

        _memoryCache.Set<List<Anime>>(cacheKey, new List<Anime>(), expiredOption);
        return new List<Anime>();
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
        var response = await this.Request(url, cancellationToken).ConfigureAwait(false);

        var result = await response.Content.ReadFromJsonAsync<AnimeResult>(_jsonOptions, cancellationToken).ConfigureAwait(false);
        if (result != null && result.Success && result.Bangumi != null)
        {
            // 过滤掉特典剧集，episodeNumber为S1/S2.。。
            anime = result.Bangumi;
            if (anime.Episodes != null)
            {
                anime.Episodes = anime.Episodes.Where(x => x.EpisodeNumber.ToInt() > 0).ToList();
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

        var withRelated = this.Config.WithRelatedDanmu ? "true" : "false";
        var chConvert = this.Config.ChConvert;
        var url = $"https://api.dandanplay.net/api/v2/comment/{epId}?withRelated={withRelated}&chConvert={chConvert}";
        var response = await this.Request(url, cancellationToken).ConfigureAwait(false);
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
        var timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
        var signature = GenerateSignature(url, timestamp);
        
        httpClient.DefaultRequestHeaders.Add("X-AppId", API_ID);
        httpClient.DefaultRequestHeaders.Add("X-Signature", signature);
        httpClient.DefaultRequestHeaders.Add("X-Timestamp", timestamp.ToString());
        var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return response;
    }

    protected string GenerateSignature(string url, long timestamp)
    {
        if (string.IsNullOrEmpty(API_ID) || string.IsNullOrEmpty(API_SECRET))
        {
            throw new Exception("弹弹接口缺少API_ID和API_SECRET");
        }
        var uri = new Uri(url);
        var path = uri.AbsolutePath;
        var str = $"{API_ID}{timestamp}{path}{API_SECRET}";
        using (var sha256 = SHA256.Create())
        {
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(str));
            return Convert.ToBase64String(hashBytes);
        }
    }

}
