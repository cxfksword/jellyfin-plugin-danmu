using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Plugin.Danmu.Core.Extensions;
using Emby.Plugin.Danmu.Core.Singleton;
using Emby.Plugin.Danmu.Scraper.Tencent.Entity;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Logging;
using Microsoft.Extensions.Caching.Memory;

namespace Emby.Plugin.Danmu.Scraper.Tencent
{
    public class TencentApi : AbstractApi
    {
        protected Dictionary<string, string> defaultHeaders;
        protected string[] cookies;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="TencentApi"/> class.
        /// </summary>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        public TencentApi(ILogManager logManager, IHttpClient httpClient)
            : base(logManager.getDefaultLogger("TencentApi"), httpClient)
        {

            this.defaultHeaders = new Dictionary<string, string>
            {
                { "referer", "https://v.qq.com/" }
            };

            this.cookies = new[]
            {
                "pgv_pvid=40b67e3b06027f3d; video_platform=2; vversion_name=8.2.95; video_bucketid=4; video_omgid=0a1ff6bc9407c0b1cff86ee5d359614d"
            };
        }


        protected override Dictionary<string, string> GetDefaultHeaders()
        {
            return defaultHeaders;
        }
        
        protected override string[] GetDefaultCookies(string? url=null)
        {
            return cookies;
        }

        public async Task<List<TencentVideo>> SearchAsync(string keyword, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(keyword))
            {
                return new List<TencentVideo>();
            }

            var cacheKey = $"search_{keyword}";
            var expiredOption = new MemoryCacheEntryOptions()
                { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) };
            if (!SingletonManager.IsDebug && _memoryCache.TryGetValue<List<TencentVideo>>(cacheKey, out var cacheValue))
            {
                return cacheValue;
            }

            await this.LimitRequestFrequently();

            var originPostData = new TencentSearchRequest() { Query = keyword };
            var url = $"https://pbaccess.video.qq.com/trpc.videosearch.mobile_search.HttpMobileRecall/MbSearchHttp";

            var result = new List<TencentVideo>();
            var searchResult = await httpClient.GetSelfResultAsyncWithError<TencentSearchResult>(GetDefaultHttpRequestOptions(url, null, cancellationToken), null, "POST", originPostData);
            
            if (searchResult != null && searchResult.Data != null && searchResult.Data.NormalList != null &&
                searchResult.Data.NormalList.ItemList != null)
            {
                foreach (var item in searchResult.Data.NormalList.ItemList)
                {
                    if (item.VideoInfo.Year == null || item.VideoInfo.Year == 0)
                    {
                        continue;
                    }
                    
                    if (item.VideoInfo.Title.Distance(keyword) <= 0)
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
            var expiredOption = new MemoryCacheEntryOptions()
                { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) };
            if (_memoryCache.TryGetValue<TencentVideo?>(cacheKey, out var video))
            {
                return video;
            }

            var originPostData = new TencentEpisodeListRequest() { PageParams = new TencentPageParams() { Cid = id } };
            var url = $"https://pbaccess.video.qq.com/trpc.universal_backend_service.page_server_rpc.PageServer/GetPageData?video_appid=3000010&vplatform=2";

            var result = await httpClient.GetSelfResultAsyncWithError<TencentEpisodeListResult>(GetDefaultHttpRequestOptions(url), null, "POST", originPostData).ConfigureAwait(false);
            if (result != null && result.Data != null && result.Data.ModuleListDatas != null)
            {
                var videoInfo = new TencentVideo();
                videoInfo.Id = id;
                videoInfo.EpisodeList = result.Data.ModuleListDatas.First().ModuleDatas.First().ItemDataLists.ItemDatas
                    .Select(x => x.ItemParams).Where(x => x.IsTrailer != "1").ToList();
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
            var result = await httpClient.GetSelfResultAsyncWithError<TencentCommentResult>(GetDefaultHttpRequestOptions(url)).ConfigureAwait(false);
            if (result != null && result.SegmentIndex != null)
            {
                var start = result.SegmentStart.ToLong();
                var size = result.SegmentSpan.ToLong();
                for (long i = start; result.SegmentIndex.ContainsKey(i) && size > 0; i += size)
                {
                    var segment = result.SegmentIndex[i];
                    var segmentUrl = $"https://dm.video.qq.com/barrage/segment/{vid}/{segment.SegmentName}";

                    var segmentResult = await httpClient
                        .GetSelfResultAsyncWithError<TencentCommentSegmentResult>(GetDefaultHttpRequestOptions(segmentUrl)).ConfigureAwait(false);
                    if (segmentResult != null && segmentResult.BarrageList != null)
                    {
                        // 30秒每segment，为避免弹幕太大，从中间隔抽取最大60秒200条弹幕
                        danmuList.AddRange(segmentResult.BarrageList.ExtractToNumber(100));
                    }

                    // 等待一段时间避免api请求太快
                    Thread.Sleep(100);
                }
            }

            return danmuList;
        }

        protected async Task LimitRequestFrequently()
        {
            Thread.Sleep(1000);
        }
    }
}