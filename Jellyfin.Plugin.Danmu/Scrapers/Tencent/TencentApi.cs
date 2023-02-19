using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using ComposableAsync;
using Jellyfin.Plugin.Danmu.Core.Extensions;
using Jellyfin.Plugin.Danmu.Scrapers.Entity;
using Jellyfin.Plugin.Danmu.Scrapers.Tencent.Entity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using RateLimiter;

namespace Jellyfin.Plugin.Danmu.Scrapers.Tencent;

public class TencentApi : AbstractApi
{
    private TimeLimiter _timeConstraint = TimeLimiter.GetFromMaxCountByInterval(1, TimeSpan.FromMilliseconds(1000));
    private TimeLimiter _delayExecuteConstraint = TimeLimiter.GetFromMaxCountByInterval(1, TimeSpan.FromMilliseconds(100));


    /// <summary>
    /// Initializes a new instance of the <see cref="TencentApi"/> class.
    /// </summary>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    public TencentApi(ILoggerFactory loggerFactory)
        : base(loggerFactory.CreateLogger<TencentApi>())
    {
        httpClient.DefaultRequestHeaders.Add("referer", "https://v.qq.com/");
        this.AddCookies("pgv_pvid=40b67e3b06027f3d; video_platform=2; vversion_name=8.2.95; video_bucketid=4; video_omgid=0a1ff6bc9407c0b1cff86ee5d359614d", new Uri("https://v.qq.com"));
    }


    public async Task<List<TencentVideo>> SearchAsync(string keyword, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(keyword))
        {
            return new List<TencentVideo>();
        }

        var cacheKey = $"search_{keyword}";
        var expiredOption = new MemoryCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) };
        if (_memoryCache.TryGetValue<List<TencentVideo>>(cacheKey, out var cacheValue))
        {
            return cacheValue;
        }

        await this.LimitRequestFrequently();

        var postData = new TencentSearchRequest() { Query = keyword };
        var url = $"https://pbaccess.video.qq.com/trpc.videosearch.mobile_search.HttpMobileRecall/MbSearchHttp";
        var response = await httpClient.PostAsJsonAsync<TencentSearchRequest>(url, postData, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var result = new List<TencentVideo>();
        var searchResult = await response.Content.ReadFromJsonAsync<TencentSearchResult>(_jsonOptions, cancellationToken).ConfigureAwait(false);
        if (searchResult != null && searchResult.Data != null && searchResult.Data.NormalList != null && searchResult.Data.NormalList.ItemList != null)
        {
            foreach (var item in searchResult.Data.NormalList.ItemList)
            {
                if (item.VideoInfo.Year == null || item.VideoInfo.Year == 0)
                {
                    continue;
                }

                var video = item.VideoInfo;
                video.Id = item.Doc.Id;
                result.Add(video);
            }
        }

        _memoryCache.Set<List<TencentVideo>>(cacheKey, result, expiredOption);
        return result;
    }

    public async Task<TencentVideo?> GetVideoAsync(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        var cacheKey = $"media_{id}";
        var expiredOption = new MemoryCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) };
        if (_memoryCache.TryGetValue<TencentVideo?>(cacheKey, out var video))
        {
            return video;
        }

        var postData = new TencentEpisodeListRequest() { PageParams = new TencentPageParams() { Cid = id } };
        var url = $"https://pbaccess.video.qq.com/trpc.universal_backend_service.page_server_rpc.PageServer/GetPageData?video_appid=3000010&vplatform=2";
        var response = await httpClient.PostAsJsonAsync<TencentEpisodeListRequest>(url, postData, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<TencentEpisodeListResult>(_jsonOptions, cancellationToken).ConfigureAwait(false);
        if (result != null && result.Data != null && result.Data.ModuleListDatas != null)
        {
            var videoInfo = new TencentVideo();
            videoInfo.Id = id;
            videoInfo.EpisodeList = result.Data.ModuleListDatas.First().ModuleDatas.First().ItemDataLists.ItemDatas.Select(x => x.ItemParams).Where(x => x.IsTrailer != "1").ToList();
            _memoryCache.Set<TencentVideo?>(cacheKey, videoInfo, expiredOption);
            return videoInfo;
        }

        _memoryCache.Set<TencentVideo?>(cacheKey, null, expiredOption);
        return null;
    }




    public async Task<List<TencentComment>> GetDanmuContentAsync(string vid, CancellationToken cancellationToken)
    {
        var danmuList = new List<TencentComment>();
        if (string.IsNullOrEmpty(vid))
        {
            return danmuList;
        }


        var url = $"https://dm.video.qq.com/barrage/base/{vid}";
        var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<TencentCommentResult>(_jsonOptions, cancellationToken).ConfigureAwait(false);
        if (result != null && result.SegmentIndex != null)
        {
            var start = result.SegmentStart.ToLong();
            var size = result.SegmentSpan.ToLong();
            for (long i = start; result.SegmentIndex.ContainsKey(i) && size > 0; i += size)
            {

                var segment = result.SegmentIndex[i];
                var segmentUrl = $"https://dm.video.qq.com/barrage/segment/{vid}/{segment.SegmentName}";
                var segmentResponse = await httpClient.GetAsync(segmentUrl, cancellationToken).ConfigureAwait(false);
                segmentResponse.EnsureSuccessStatusCode();

                var segmentResult = await segmentResponse.Content.ReadFromJsonAsync<TencentCommentSegmentResult>(_jsonOptions, cancellationToken).ConfigureAwait(false);
                if (segmentResult != null && segmentResult.BarrageList != null)
                {
                    // 30秒每segment，为避免弹幕太大，从中间隔抽取最大60秒200条弹幕
                    danmuList.AddRange(segmentResult.BarrageList.ExtractToNumber(100));
                }

                // 等待一段时间避免api请求太快
                await _delayExecuteConstraint;
            }
        }

        return danmuList;
    }

    protected async Task LimitRequestFrequently()
    {
        await this._timeConstraint;
    }

}

