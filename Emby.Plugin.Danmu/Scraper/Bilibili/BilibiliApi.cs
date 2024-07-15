using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Emby.Plugin.Danmu.Core.Extensions;
using Emby.Plugin.Danmu.Scraper.Bilibili.Entity;
using Emby.Plugin.Danmu.Scraper.Entity;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Logging;
using Microsoft.Extensions.Caching.Memory;

namespace Emby.Plugin.Danmu.Scraper.Bilibili
{
    public class BilibiliApi : AbstractApi
    {
        private static readonly Regex regBiliplusVideoInfo = new Regex(@"view\((.+?)\);", RegexOptions.Compiled);

        /// <summary>
        /// Initializes a new instance of the <see cref="BilibiliApi"/> class.
        /// </summary>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        public BilibiliApi(ILogManager logManager, IHttpClient httpClient)
            : base(logManager.getDefaultLogger("BilibiliApi"), httpClient)
        {
        }


        public async Task<SearchResult> SearchAsync(string keyword, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(keyword))
            {
                return new SearchResult();
            }

            var cacheKey = $"search_{keyword}";
            var expiredOption = new MemoryCacheEntryOptions()
                { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) };
            if (this._memoryCache.TryGetValue<SearchResult>(cacheKey, out var searchResult))
            {
                return searchResult;
            }

            await this.LimitRequestFrequently();
            await this.EnsureSessionCookie(cancellationToken).ConfigureAwait(false);

            keyword = HttpUtility.UrlEncode(keyword);

            // 搜索影视
            var result = new SearchResult();
            var url = $"https://api.bilibili.com/x/web-interface/search/type?keyword={keyword}&search_type=media_ft";
            HttpRequestOptions httpRequestOptions = GetDefaaultHttpRequestOptions(url, null, cancellationToken);
            var ftResult = await httpClient.GetSelfResultAsyncWithError<ApiResult<SearchResult>>(httpRequestOptions)
                .ConfigureAwait(false);
            if (ftResult != null && ftResult.Code == 0 && ftResult.Data != null)
            {
                result = ftResult.Data;
            }

            // 搜索番剧
            url = $"https://api.bilibili.com/x/web-interface/search/type?keyword={keyword}&search_type=media_bangumi";
            httpRequestOptions.Url = url;
            var bangumiResult = await this.httpClient.GetSelfResultAsyncWithError<ApiResult<SearchResult>>(httpRequestOptions).ConfigureAwait(false);
            if (bangumiResult != null && bangumiResult.Code == 0 && bangumiResult.Data != null &&
                bangumiResult.Data.Result != null)
            {
                if (result.Result == null)
                {
                    result = bangumiResult.Data;
                }
                else
                {
                    result.Result.AddRange(bangumiResult.Data.Result);
                }
            }


            this._memoryCache.Set<SearchResult>(cacheKey, result, expiredOption);
            return result;
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
            HttpRequestOptions httpRequestOptions = GetDefaaultHttpRequestOptions(pageUrl, null, cancellationToken);
            var result = await this.httpClient.GetSelfResultAsyncWithError<ApiResult<VideoPart[]>>(httpRequestOptions).ConfigureAwait(false);
            if (result != null && result.Code == 0 && result.Data != null)
            {
                var part = result.Data.FirstOrDefault();
                if (part != null)
                {
                    return await this.GetDanmuContentByCidAsync(part.Cid, cancellationToken).ConfigureAwait(false);
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


            var episode = await this.GetEpisodeAsync(epId, cancellationToken).ConfigureAwait(false);
            if (episode != null)
            {
                return await this.GetDanmuContentByCidAsync(episode.CId, cancellationToken).ConfigureAwait(false);
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
            var response = await this.httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
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


        public async Task<VideoSeason?> GetSeasonAsync(long seasonId, CancellationToken cancellationToken)
        {
            if (seasonId <= 0)
            {
                return null;
            }

            var cacheKey = $"season_{seasonId}";
            var expiredOption = new MemoryCacheEntryOptions()
                { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) };
            VideoSeason? seasonData;
            if (this._memoryCache.TryGetValue<VideoSeason?>(cacheKey, out seasonData))
            {
                return seasonData;
            }

            await this.EnsureSessionCookie(cancellationToken).ConfigureAwait(false);

            var url = $"http://api.bilibili.com/pgc/view/web/season?season_id={seasonId}";
            
            var result = await this.httpClient.GetSelfResultAsyncWithError<ApiResult<VideoSeason>>(GetDefaaultHttpRequestOptions(url, null, cancellationToken)).ConfigureAwait(false);
            if (result != null && result.Code == 0 && result.Result != null)
            {
                this._memoryCache.Set<VideoSeason?>(cacheKey, result.Result, expiredOption);
                return result.Result;
            }

            this._memoryCache.Set<VideoSeason?>(cacheKey, null, expiredOption);
            return null;
        }

        public async Task<VideoEpisode?> GetEpisodeAsync(long epId, CancellationToken cancellationToken)
        {
            if (epId <= 0)
            {
                return null;
            }

            var cacheKey = $"episode_{epId}";
            var expiredOption = new MemoryCacheEntryOptions()
                { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) };
            VideoEpisode? episodeData;
            if (this._memoryCache.TryGetValue<VideoEpisode?>(cacheKey, out episodeData))
            {
                return episodeData;
            }

            await this.EnsureSessionCookie(cancellationToken).ConfigureAwait(false);

            var url = $"http://api.bilibili.com/pgc/view/web/season?ep_id={epId}";
            var result = await this.httpClient.GetSelfResultAsyncWithError<ApiResult<VideoSeason>>(GetDefaaultHttpRequestOptions(url, null, cancellationToken)).ConfigureAwait(false);
            if (result != null && result.Code == 0 && result.Result != null && result.Result.Episodes != null)
            {
                // 缓存本季的所有episode数据，避免批量更新时重复请求
                foreach (var episode in result.Result.Episodes)
                {
                    cacheKey = $"episode_{episode.Id}";
                    this._memoryCache.Set<VideoEpisode?>(cacheKey, episode, expiredOption);
                }

                return result.Result.Episodes.FirstOrDefault(x => x.Id == epId);
            }

            this._memoryCache.Set<VideoEpisode?>(cacheKey, null, expiredOption);
            return null;
        }

        public async Task<Video?> GetVideoByBvidAsync(string bvid, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(bvid))
            {
                return null;
            }

            var cacheKey = $"video_{bvid}";
            var expiredOption = new MemoryCacheEntryOptions()
                { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) };
            Video? videoData;
            if (this._memoryCache.TryGetValue<Video?>(cacheKey, out videoData))
            {
                return videoData;
            }

            await this.EnsureSessionCookie(cancellationToken).ConfigureAwait(false);

            var url = $"https://api.bilibili.com/x/web-interface/view?bvid={bvid}";
            var result = await this.httpClient.GetSelfResultAsyncWithError<ApiResult<Video>>(GetDefaaultHttpRequestOptions(url, null, cancellationToken)).ConfigureAwait(false);
            if (result != null && result.Code == 0 && result.Data != null)
            {
                this._memoryCache.Set<Video?>(cacheKey, result.Data, expiredOption);
                return result.Data;
            }

            this._memoryCache.Set<Video?>(cacheKey, null, expiredOption);
            return null;
        }


        public async Task<BiliplusVideo?> GetVideoByAvidAsync(string avid, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(avid))
            {
                return null;
            }

            var cacheKey = $"video_{avid}";
            var expiredOption = new MemoryCacheEntryOptions()
                { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) };
            BiliplusVideo? videoData;
            if (this._memoryCache.TryGetValue<BiliplusVideo?>(cacheKey, out videoData))
            {
                return videoData;
            }

            var url = $"https://www.biliplus.com/video/{avid}/";
            var videoInfo = await this.httpClient.GetSelfResultAsyncWithError<BiliplusVideo>(GetDefaaultHttpRequestOptions(url, null, cancellationToken),
                response =>
                {
                    // 确保响应状态码为成功
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        // 读取响应流
                        using (var responseStream = response.Content)
                        using (var reader = new StreamReader(responseStream, Encoding.UTF8))
                        {
                            var htmlResult = reader.ReadToEnd();
                            var videoJson = regBiliplusVideoInfo.FirstMatchGroup(htmlResult);
                            return videoJson;
                        }
                    }

                    return null;
                    // var body = response.Content.ReadFromJsonAsync<>(cancellationToken).ConfigureAwait(false);
                }).ConfigureAwait(false);

            if (videoInfo != null)
            {
                this._memoryCache.Set<BiliplusVideo?>(cacheKey, videoInfo, expiredOption);
                return videoInfo;
            }

            this._memoryCache.Set<BiliplusVideo?>(cacheKey, null, expiredOption);
            return null;
        }

        /// <summary>
        /// 下载实时弹幕，返回弹幕列表
        /// protobuf定义：https://github.com/SocialSisterYi/bilibili-API-collect/blob/master/grpc_api/bilibili/community/service/dm/v1/dm.proto
        /// </summary>
        /// <param name="aid">稿件avID</param>
        /// <param name="cid">视频CID</param>
        public async Task<ScraperDanmaku?> GetDanmuContentByProtoAsync(long aid, long cid,
            CancellationToken cancellationToken)
        {
            var danmaku = new ScraperDanmaku();
            danmaku.ChatId = cid;
            danmaku.ChatServer = "api.bilibili.com";
            danmaku.Items = new List<ScraperDanmakuText>();

            await this.EnsureSessionCookie(cancellationToken).ConfigureAwait(false);

            try
            {
                var segmentIndex = 1; // 分包，每6分钟一包
                while (true)
                {
                    var url =
                        $"https://api.bilibili.com/x/v2/dm/web/seg.so?type=1&oid={cid}&pid={aid}&segment_index={segmentIndex}";
                    var response = await this.httpClient.GetAsync(GetDefaaultHttpRequestOptions(url, null, cancellationToken)).ConfigureAwait(false);
                    if (response.StatusCode == System.Net.HttpStatusCode.NotModified)
                    {
                        // 已经到最后了
                        break;
                    }

                    if (response.Headers.TryGetValue("bili-status-code", out var headers))
                    {
                        var biliStatusCode = headers.FirstOrDefault();
                        if (biliStatusCode == "-352")
                        {
                            this._logger.LogWarning($"下载部分弹幕失败. bili-status-code: {biliStatusCode} url: {url}");
                            danmaku.Items = new List<ScraperDanmakuText>();
                            return danmaku;
                        }
                    }

                    response.EnsureSuccessStatusCode();
                    var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
                    var danmuReply = Biliproto.Community.Service.Dm.V1.DmSegMobileReply.Parser.ParseFrom(bytes);
                    if (danmuReply == null || danmuReply.Elems == null || danmuReply.Elems.Count <= 0)
                    {
                        break;
                    }

                    var segmentList = new List<ScraperDanmakuText>();
                    foreach (var dm in danmuReply.Elems)
                    {
                        // <d p="944.95400,5,25,16707842,1657598634,0,ece5c9d1,1094775706690331648,11">今天的风儿甚是喧嚣</d>
                        // time, mode, size, color, create, pool, sender, id, weight(屏蔽等级)
                        segmentList.Add(new ScraperDanmakuText()
                        {
                            Id = dm.Id,
                            Progress = dm.Progress,
                            Mode = dm.Mode,
                            Fontsize = dm.Fontsize,
                            Color = dm.Color,
                            MidHash = dm.MidHash,
                            Content = dm.Content,
                            Ctime = dm.Ctime,
                            Weight = dm.Weight,
                            Pool = dm.Pool,
                        });
                    }

                    // 每段有6分钟弹幕，为避免弹幕太大，从中间隔抽取最大60秒200条弹幕
                    danmaku.Items.AddRange(segmentList.ExtractToNumber(1200));
                    segmentIndex += 1;

                    // 等待一段时间避免api请求太快
                    await this._delayExecuteConstraint;
                }
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "下载弹幕出错");
            }

            return danmaku;
        }

        private async Task EnsureSessionCookie(CancellationToken cancellationToken)
        {
            var url = "https://www.bilibili.com";
            var cookies = this._cookieContainer.GetCookies(new Uri(url, UriKind.Absolute));
            var existCookie = cookies.FirstOrDefault(x => x.Name == "buvid3");
            if (existCookie != null)
            {
                return;
            }

            var response = await this.httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }

        protected async Task LimitRequestFrequently()
        {
            await this._timeConstraint;
        }
    }
}