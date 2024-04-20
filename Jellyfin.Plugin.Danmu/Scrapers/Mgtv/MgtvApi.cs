using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using ComposableAsync;
using Jellyfin.Plugin.Danmu.Core.Extensions;
using Jellyfin.Plugin.Danmu.Scrapers.Mgtv.Entity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using RateLimiter;

namespace Jellyfin.Plugin.Danmu.Scrapers.Mgtv;

public class MgtvApi : AbstractApi
{
    private TimeLimiter _timeConstraint = TimeLimiter.GetFromMaxCountByInterval(1, TimeSpan.FromMilliseconds(1000));
    private TimeLimiter _delayExecuteConstraint = TimeLimiter.GetFromMaxCountByInterval(1, TimeSpan.FromMilliseconds(100));

    /// <summary>
    /// Initializes a new instance of the <see cref="MgtvApi"/> class.
    /// </summary>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    public MgtvApi(ILoggerFactory loggerFactory)
        : base(loggerFactory.CreateLogger<MgtvApi>())
    {
        httpClient.DefaultRequestHeaders.Add("referer", "https://www.mgtv.com/");
    }


    public async Task<List<MgtvSearchItem>> SearchAsync(string keyword, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(keyword))
        {
            return new List<MgtvSearchItem>();
        }

        var cacheKey = $"search_{keyword}";
        var expiredOption = new MemoryCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) };
        if (_memoryCache.TryGetValue<List<MgtvSearchItem>>(cacheKey, out var cacheValue))
        {
            return cacheValue;
        }

        await this.LimitRequestFrequently();

        keyword = HttpUtility.UrlEncode(keyword);
        var url = $"https://mobileso.bz.mgtv.com/msite/search/v2?q={keyword}&pc=30&pn=1&sort=-99&ty=0&du=0&pt=0&corr=1&abroad=0&_support=10000000000000000";
        var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var result = new List<MgtvSearchItem>();
        var searchResult = await response.Content.ReadFromJsonAsync<MgtvSearchResult>(_jsonOptions, cancellationToken).ConfigureAwait(false);
        if (searchResult != null && searchResult.Data != null && searchResult.Data.Contents != null)
        {
            foreach (var content in searchResult.Data.Contents)
            {
                if (content.Type != "media")
                {
                    continue;
                }
                foreach (var item in content.Data)
                {
                    if (string.IsNullOrEmpty(item.Id))
                    {
                        continue;
                    }

                    result.Add(item);
                }
            }
        }

        _memoryCache.Set<List<MgtvSearchItem>>(cacheKey, result, expiredOption);
        return result;
    }

    public async Task<MgtvVideo?> GetVideoAsync(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        var cacheKey = $"media_{id}";
        var expiredOption = new MemoryCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) };
        if (_memoryCache.TryGetValue<MgtvVideo?>(cacheKey, out var video))
        {
            return video;
        }

        var month = "";
        var idx = 0;
        var total = 0;
        var videoInfo = new MgtvVideo() { Id = id };
        var list = new List<MgtvEpisode>();
        do
        {
            var url = $"https://pcweb.api.mgtv.com/variety/showlist?allowedRC=1&collection_id={id}&month={month}&page=1&_support=10000000";
            var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<MgtvEpisodeListResult>(_jsonOptions, cancellationToken).ConfigureAwait(false);
            if (result != null && result.Data != null && result.Data.List != null)
            {
                list.AddRange(result.Data.List.Where(x => x.SourceClipId == id));

                total = result.Data.Tabs.Count;
                idx++;
                month = idx < total ? result.Data.Tabs[idx].Month : "";
            }

            // 等待一段时间避免api请求太快
            await _delayExecuteConstraint;
        } while (idx < total && !string.IsNullOrEmpty(month));

        videoInfo.EpisodeList = list.OrderBy(x => x.VideoId).ToList();
        _memoryCache.Set<MgtvVideo?>(cacheKey, videoInfo, expiredOption);
        return videoInfo;
    }




    public async Task<List<MgtvComment>> GetDanmuContentAsync(string cid, string vid, CancellationToken cancellationToken)
    {
        var danmuList = new List<MgtvComment>();
        if (string.IsNullOrEmpty(vid))
        {
            return danmuList;
        }


        var time = 0;
        do
        {
            var segmentUrl = $"https://galaxy.bz.mgtv.com/cdn/opbarrage?vid={vid}&pid=&cid={cid}&ticket=&time={time}&allowedRC=1";
            var segmentResponse = await this.httpClient.GetAsync(segmentUrl, cancellationToken).ConfigureAwait(false);
            segmentResponse.EnsureSuccessStatusCode();

            var segmentResult = await segmentResponse.Content.ReadFromJsonAsync<MgtvCommentSegmentResult>(_jsonOptions, cancellationToken).ConfigureAwait(false);
            if (segmentResult != null && segmentResult.Data != null && segmentResult.Data.Items != null)
            {
                // 60秒每segment，为避免弹幕太大，从中间隔抽取最大60秒200条弹幕
                danmuList.AddRange(segmentResult.Data.Items.ExtractToNumber(200));
            }
            else
            {
                break;
            }

            time = segmentResult?.Data?.Next ?? 0;
            // 等待一段时间避免api请求太快
            await _delayExecuteConstraint;
        }
        while (time > 0);
    

        return danmuList;
    }

    protected async Task LimitRequestFrequently()
    {
        await this._timeConstraint;
    }

}

