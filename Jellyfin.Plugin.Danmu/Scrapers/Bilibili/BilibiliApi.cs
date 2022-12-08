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
using Jellyfin.Plugin.Danmu.Core.Http;
using Jellyfin.Plugin.Danmu.Scrapers.Bilibili.Entity;

namespace Jellyfin.Plugin.Danmu.Scrapers.Bilibili;

public class BilibiliApi : IDisposable
{
    const string HTTP_USER_AGENT = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/93.0.4577.63 Safari/537.36 Edg/93.0.961.44";
    private readonly ILogger<BilibiliApi> _logger;
    private readonly JsonSerializerOptions _jsonOptions = JsonDefaults.Options;
    private HttpClient httpClient;
    private CookieContainer _cookieContainer;
    private readonly IMemoryCache _memoryCache;
    private static readonly object _lock = new object();
    private DateTime lastRequestTime = DateTime.Now.AddDays(-1);

    /// <summary>
    /// Initializes a new instance of the <see cref="BilibiliApi"/> class.
    /// </summary>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    public BilibiliApi(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<BilibiliApi>();

        var handler = new HttpClientHandlerEx();
        _cookieContainer = handler.CookieContainer;
        httpClient = new HttpClient(handler, true);
        httpClient.DefaultRequestHeaders.Add("user-agent", HTTP_USER_AGENT);
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
    }

    /// <summary>
    /// Get bilibili danmu data.
    /// </summary>
    /// <param name="bvid">The Bilibili bvid.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>Task{TraktResponseDataContract}.</returns>
    public async Task<byte[]> GetDanmuContentAsync(string bvid, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(bvid))
        {
            throw new ArgumentNullException(nameof(bvid));
        }

        // http://api.bilibili.com/x/player/pagelist?bvid={bvid}
        // https://api.bilibili.com/x/v1/dm/list.so?oid={cid}
        bvid = bvid.Trim();
        var pageUrl = $"http://api.bilibili.com/x/player/pagelist?bvid={bvid}";
        var response = await httpClient.GetAsync(pageUrl, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ApiResult<VideoPart[]>>(_jsonOptions, cancellationToken).ConfigureAwait(false);
        if (result != null && result.Code == 0 && result.Data != null)
        {
            var part = result.Data.FirstOrDefault();
            if (part != null)
            {
                return await GetDanmuContentByCidAsync(part.Cid, cancellationToken).ConfigureAwait(false);
            }
        }

        throw new Exception($"Request fail. bvid={bvid}");
    }

    /// <summary>
    /// Get bilibili danmu data.
    /// </summary>
    /// <param name="bvid">The Bilibili bvid.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>Task{TraktResponseDataContract}.</returns>
    public async Task<byte[]> GetDanmuContentAsync(long epId, CancellationToken cancellationToken)
    {
        if (epId <= 0)
        {
            throw new ArgumentNullException(nameof(epId));
        }


        var season = await GetEpisodeAsync(epId, cancellationToken).ConfigureAwait(false);
        if (season != null && season.Episodes.Length > 0)
        {
            var episode = season.Episodes.First(x => x.Id == epId);
            if (episode != null)
            {
                return await GetDanmuContentByCidAsync(episode.CId, cancellationToken).ConfigureAwait(false);
            }
        }

        throw new Exception($"Request fail. epId={epId}");
    }

    public async Task<byte[]> GetDanmuContentByCidAsync(long cid, CancellationToken cancellationToken)
    {
        if (cid <= 0)
        {
            throw new ArgumentNullException(nameof(cid));
        }

        var url = $"https://api.bilibili.com/x/v1/dm/list.so?oid={cid}";
        var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Request fail. url={url} status_code={response.StatusCode}");
        }

        // 数据太小可能是已经被b站下架，返回了出错信息
        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
        if (bytes == null || bytes.Length < 2000)
        {
            throw new Exception($"弹幕获取失败，可能视频已下架或弹幕太少. url: {url}");
        }

        return bytes;
    }


    public async Task<SearchResult> SearchAsync(string keyword, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(keyword))
        {
            return new SearchResult();
        }

        var cacheKey = $"search_{keyword}";
        var expiredOption = new MemoryCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) };
        SearchResult searchResult;
        if (_memoryCache.TryGetValue<SearchResult>(cacheKey, out searchResult))
        {
            return searchResult;
        }

        this.LimitRequestFrequently();
        await EnsureSessionCookie(cancellationToken).ConfigureAwait(false);

        keyword = HttpUtility.UrlEncode(keyword);
        var url = $"http://api.bilibili.com/x/web-interface/search/all/v2?keyword={keyword}";
        var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ApiResult<SearchResult>>(_jsonOptions, cancellationToken).ConfigureAwait(false);
        if (result != null && result.Code == 0 && result.Data != null)
        {
            _memoryCache.Set<SearchResult>(cacheKey, result.Data, expiredOption);
            return result.Data;
        }

        _memoryCache.Set<SearchResult>(cacheKey, new SearchResult(), expiredOption);
        return new SearchResult();
    }

    public async Task<VideoSeason?> GetSeasonAsync(long seasonId, CancellationToken cancellationToken)
    {
        if (seasonId <= 0)
        {
            return null;
        }

        var cacheKey = $"season_{seasonId}";
        var expiredOption = new MemoryCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) };
        VideoSeason? seasonData;
        if (_memoryCache.TryGetValue<VideoSeason?>(cacheKey, out seasonData))
        {
            return seasonData;
        }

        await EnsureSessionCookie(cancellationToken).ConfigureAwait(false);

        var url = $"http://api.bilibili.com/pgc/view/web/season?season_id={seasonId}";
        var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ApiResult<VideoSeason>>(_jsonOptions, cancellationToken).ConfigureAwait(false);
        if (result != null && result.Code == 0 && result.Result != null)
        {
            _memoryCache.Set<VideoSeason?>(cacheKey, result.Result, expiredOption);
            return result.Result;
        }

        _memoryCache.Set<VideoSeason?>(cacheKey, null, expiredOption);
        return null;
    }

    public async Task<VideoSeason?> GetEpisodeAsync(long epId, CancellationToken cancellationToken)
    {
        if (epId <= 0)
        {
            return null;
        }

        var cacheKey = $"episode_{epId}";
        var expiredOption = new MemoryCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) };
        VideoSeason? seasonData;
        if (_memoryCache.TryGetValue<VideoSeason?>(cacheKey, out seasonData))
        {
            return seasonData;
        }

        await EnsureSessionCookie(cancellationToken).ConfigureAwait(false);

        var url = $"http://api.bilibili.com/pgc/view/web/season?ep_id={epId}";
        var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ApiResult<VideoSeason>>(_jsonOptions, cancellationToken).ConfigureAwait(false);
        if (result != null && result.Code == 0 && result.Result != null)
        {
            _memoryCache.Set<VideoSeason?>(cacheKey, result.Result, expiredOption);
            return result.Result;
        }

        _memoryCache.Set<VideoSeason?>(cacheKey, null, expiredOption);
        return null;
    }

    public async Task<Video?> GetVideoByBvidAsync(string bvid, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(bvid))
        {
            return null;
        }

        var cacheKey = $"video_{bvid}";
        var expiredOption = new MemoryCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) };
        Video? videoData;
        if (_memoryCache.TryGetValue<Video?>(cacheKey, out videoData))
        {
            return videoData;
        }

        await EnsureSessionCookie(cancellationToken).ConfigureAwait(false);

        var url = $"https://api.bilibili.com/x/web-interface/view?bvid={bvid}";
        var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ApiResult<Video>>(_jsonOptions, cancellationToken).ConfigureAwait(false);
        if (result != null && result.Code == 0 && result.Data != null)
        {
            _memoryCache.Set<Video?>(cacheKey, result.Data, expiredOption);
            return result.Data;
        }

        _memoryCache.Set<Video?>(cacheKey, null, expiredOption);
        return null;
    }

    private async Task EnsureSessionCookie(CancellationToken cancellationToken)
    {
        var url = "https://www.bilibili.com";
        var cookies = _cookieContainer.GetCookies(new Uri(url, UriKind.Absolute));
        var existCookie = cookies.FirstOrDefault(x => x.Name == "buvid3");
        if (existCookie != null)
        {
            return;
        }

        var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    protected void LimitRequestFrequently()
    {
        var diff = 0;
        lock (_lock)
        {
            var ts = DateTime.Now - lastRequestTime;
            diff = (int)(1000 - ts.TotalMilliseconds);
            lastRequestTime = DateTime.Now;
        }

        if (diff > 0)
        {
            this._logger.LogDebug("请求太频繁，等待{0}毫秒后继续执行...", diff);
            Thread.Sleep(diff);
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _memoryCache.Dispose();
        }
    }
}

