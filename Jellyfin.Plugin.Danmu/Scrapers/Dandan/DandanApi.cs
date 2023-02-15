using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Extensions.Json;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller;
using Microsoft.Extensions.Logging;
using Jellyfin.Plugin.Danmu.Model;
using System.Threading;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Common.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using System.Net;
using System.Web;
using Microsoft.Extensions.Caching.Memory;
using Jellyfin.Plugin.Danmu.Scrapers.Dandan.Entity;
using Jellyfin.Plugin.Danmu.Configuration;

namespace Jellyfin.Plugin.Danmu.Scrapers.Dandan;

public class DandanApi : AbstractApi
{
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
        List<Anime> searchResult;
        if (_memoryCache.TryGetValue<List<Anime>>(cacheKey, out searchResult))
        {
            return searchResult;
        }

        this.LimitRequestFrequently();

        keyword = HttpUtility.UrlEncode(keyword);
        var url = $"https://api.dandanplay.net/api/v2/search/anime?keyword={keyword}";
        var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
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
        Anime? anime;
        if (_memoryCache.TryGetValue<Anime?>(cacheKey, out anime))
        {
            return anime;
        }

        var url = $"https://api.dandanplay.net/api/v2/bangumi/{animeId}";
        var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AnimeResult>(_jsonOptions, cancellationToken).ConfigureAwait(false);
        if (result != null && result.Success)
        {
            _memoryCache.Set<Anime?>(cacheKey, result.Bangumi, expiredOption);
            return result.Bangumi;
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
        var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
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

}
