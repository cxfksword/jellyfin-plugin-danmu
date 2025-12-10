using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Jellyfin.Plugin.Danmu.Scrapers.DanmuApi.Entity;
using Jellyfin.Plugin.Danmu.Configuration;
using RateLimiter;
using ComposableAsync;

namespace Jellyfin.Plugin.Danmu.Scrapers.DanmuApi;

public class DanmuApiApi : AbstractApi
{
    private TimeLimiter _timeConstraint = TimeLimiter.GetFromMaxCountByInterval(12, TimeSpan.FromMinutes(1));

    public DanmuApiOption Config
    {
        get
        {
            return Plugin.Instance?.Configuration.DanmuApi ?? new DanmuApiOption();
        }
    }

    public string ServerUrl
    {
        get
        {
            var serverUrl = Config.ServerUrl?.Trim();
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

    /// <summary>
    /// Initializes a new instance of the <see cref="DanmuApiApi"/> class.
    /// </summary>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    public DanmuApiApi(ILoggerFactory loggerFactory)
        : base(loggerFactory.CreateLogger<DanmuApiApi>())
    {
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// 搜索动漫
    /// GET /api/v2/search/anime?keyword={keyword}
    /// </summary>
    public async Task<List<Anime>> SearchAsync(string keyword, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(keyword) || string.IsNullOrEmpty(ServerUrl))
        {
            return new List<Anime>();
        }

        var cacheKey = $"danmuapi_search_{keyword}";
        var expiredOption = new MemoryCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) };
        if (_memoryCache.TryGetValue<List<Anime>>(cacheKey, out var searchResult))
        {
            return searchResult;
        }

        keyword = HttpUtility.UrlEncode(keyword);
        var url = $"{ServerUrl}/api/v2/search/anime?keyword={keyword}";
        
        try
        {
            using var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<SearchResponse>(_jsonOptions, cancellationToken).ConfigureAwait(false);
            if (result != null && result.Success && result.Animes != null)
            {
                _memoryCache.Set(cacheKey, result.Animes, expiredOption);
                return result.Animes;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DanmuApi 搜索失败: {Keyword}", keyword);
        }

        var emptyList = new List<Anime>();
        _memoryCache.Set(cacheKey, emptyList, expiredOption);
        return emptyList;
    }

    /// <summary>
    /// 获取番剧详情和剧集列表
    /// GET /api/v2/bangumi/{id}
    /// </summary>
    public async Task<Bangumi?> GetBangumiAsync(string bangumiId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(bangumiId) || string.IsNullOrEmpty(ServerUrl))
        {
            return null;
        }

        var cacheKey = $"danmuapi_bangumi_{bangumiId}";
        var expiredOption = new MemoryCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) };
        if (_memoryCache.TryGetValue<Bangumi?>(cacheKey, out var bangumi))
        {
            return bangumi;
        }

        var url = $"{ServerUrl}/api/v2/bangumi/{bangumiId}";
        
        try
        {
            using var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<BangumiResponse>(_jsonOptions, cancellationToken).ConfigureAwait(false);
            if (result != null && result.Success && result.Bangumi != null)
            {
                _memoryCache.Set(cacheKey, result.Bangumi, expiredOption);
                return result.Bangumi;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DanmuApi 获取番剧详情失败: {BangumiId}", bangumiId);
        }

        _memoryCache.Set<Bangumi?>(cacheKey, null, expiredOption);
        return null;
    }

    /// <summary>
    /// 获取弹幕内容
    /// GET /api/v2/comment/{id}
    /// </summary>
    public async Task<List<Comment>> GetCommentsAsync(string commentId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(commentId) || string.IsNullOrEmpty(ServerUrl))
        {
            return new List<Comment>();
        }

        await this.LimitRequestFrequently();

        var url = $"{ServerUrl}/api/v2/comment/{commentId}";
        
        const int maxRetries = 3;
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                using var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
                
                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    if (attempt < maxRetries - 1)
                    {
                        _logger.LogWarning("DanmuApi 获取弹幕遇到429限流,等待31秒后重试 (尝试 {Attempt}/{MaxRetries}): {CommentId}", attempt + 1, maxRetries, commentId);
                        await Task.Delay(TimeSpan.FromSeconds(31), cancellationToken);
                        continue;
                    }
                    else
                    {
                        _logger.LogError("DanmuApi 获取弹幕遇到429限流,已达到最大重试次数: {CommentId}", commentId);
                        break;
                    }
                }
                
                response.EnsureSuccessStatusCode();
                
                var result = await response.Content.ReadFromJsonAsync<CommentResponse>(_jsonOptions, cancellationToken).ConfigureAwait(false);
                if (result != null && result.Comments != null)
                {
                    return result.Comments;
                }
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DanmuApi 获取弹幕失败: {CommentId}", commentId);
                break;
            }
        }

        return new List<Comment>();
    }

    protected async Task LimitRequestFrequently()
    {
        await this._timeConstraint;
    }
}
