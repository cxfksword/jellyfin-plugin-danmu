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
using Jellyfin.Plugin.Danmu.Api.Entity;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using System.Net;
using Jellyfin.Plugin.Danmu.Api.Http;
using System.Web;
using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;
using Microsoft.Extensions.Caching.Memory;
using Jellyfin.Plugin.Danmu.Providers;
using Bilibili.Community.Service.Dm.V1;
using System.IO;
using Danmaku2Ass;
using Jellyfin.Plugin.Danmu.Core.Danmaku2Ass;

namespace Jellyfin.Plugin.Danmu.Api
{
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
                throw new Exception($"Request fail. cid={cid}");
            }

            // 数据太小可能是已经被b站下架，返回了出错信息
            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
            if (bytes == null || bytes.Length < 2000)
            {
                this._logger.LogWarning("弹幕获取失败，可能视频已下架或弹幕太少. url: {0}", url);
                throw new Exception($"Request fail. cid={cid}");
            }

            return bytes;
        }


        public async Task<SearchResult> SearchAsync(string keyword, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(keyword))
            {
                throw new ArgumentNullException(nameof(keyword));
            }

            var cacheKey = $"search_{keyword}";
            var expiredOption = new MemoryCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) };
            SearchResult searchResult;
            if (_memoryCache.TryGetValue<SearchResult>(cacheKey, out searchResult))
            {
                return searchResult;
            }

            // this.LimitRequestFrequently();
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

        /// <summary>
        /// 下载实时弹幕，返回弹幕列表
        /// </summary>
        /// <param name="avid">稿件avID</param>
        /// <param name="cid">视频CID</param>
        public async Task<byte[]> GetDanmuProtoAsync(long avid, long cid, CancellationToken cancellationToken)
        {

            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?><i>");
            try
            {
                var segmentIndex = 1;  // 分包，每6分钟一包
                while (true)
                {
                    var url = $"https://api.bilibili.com/x/v2/dm/web/seg.so?type=1&oid={cid}&pid={avid}&segment_index={segmentIndex}";

                    var bytes = await httpClient.GetByteArrayAsync(url, cancellationToken).ConfigureAwait(false);
                    var danmuReply = DmSegMobileReply.Parser.ParseFrom(bytes);
                    if (danmuReply == null)
                    {
                        break;
                    }
                    foreach (var dm in danmuReply.Elems)
                    {
                        // <d p="944.95400,5,25,16707842,1657598634,0,ece5c9d1,1094775706690331648,11">今天的风儿甚是喧嚣</d>
                        // time, mode, size, color, create, pool, sender, id, weight(屏蔽等级)
                        var str = string.Format("<d p=\"{0:0.#####},{1},{2},{3},{4},{5},{6},{7},{8}\">{9}</d>", (double)dm.Progress / 1000, dm.Mode, dm.Fontsize, dm.Color, dm.Ctime, dm.Pool, dm.MidHash, dm.IdStr, dm.Weight, dm.Content);
                        sb.AppendFormat("<d p=\"{0},{1},{2},{3},{4},{5},{6},{7},{8}\">{9}</d>", (double)dm.Progress / 1000, dm.Mode, dm.Fontsize, dm.Color, dm.Ctime, dm.Pool, dm.MidHash, dm.IdStr, dm.Weight, dm.Content);
                    }

                    segmentIndex += 1;
                }
            } catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            sb.Append("</i>");
            return Encoding.UTF8.GetBytes(sb.ToString());
        }


        /// <summary>
        /// 下载历史弹幕，返回弹幕列表
        /// </summary>
        /// <param name="cid">视频CID</param>
        /// <param name="date">弹幕日期，格式：YYYY-MM-DD</param>
        public async Task<byte[]> GetDanmuHistoryProtoAsync(long cid, string date, CancellationToken cancellationToken)
        {

            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?><i>");
            try
            {
                var segmentIndex = 1;  // 分包，每6分钟一包
                while (true)
                {
                    var url = $"http://api.bilibili.com/x/v2/dm/web/history/seg.so?type=1&oid={cid}&date={date}";

                    var bytes = await httpClient.GetByteArrayAsync(url, cancellationToken).ConfigureAwait(false);
                    var danmuReply = DmSegMobileReply.Parser.ParseFrom(bytes);
                    if (danmuReply == null)
                    {
                        break;
                    }
                    foreach (var dm in danmuReply.Elems)
                    {
                        // <d p="944.95400,5,25,16707842,1657598634,0,ece5c9d1,1094775706690331648,11">今天的风儿甚是喧嚣</d>
                        // time, mode, size, color, create, pool, sender, id, weight(屏蔽等级)
                        var str = string.Format("<d p=\"{0:0.#####},{1},{2},{3},{4},{5},{6},{7},{8}\">{9}</d>", (double)dm.Progress / 1000, dm.Mode, dm.Fontsize, dm.Color, dm.Ctime, dm.Pool, dm.MidHash, dm.IdStr, dm.Weight, dm.Content);
                        sb.AppendFormat("<d p=\"{0},{1},{2},{3},{4},{5},{6},{7},{8}\">{9}</d>", (double)dm.Progress / 1000, dm.Mode, dm.Fontsize, dm.Color, dm.Ctime, dm.Pool, dm.MidHash, dm.IdStr, dm.Weight, dm.Content);
                    }

                    segmentIndex += 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            sb.Append("</i>");
            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        public async Task<String> CheckDanmuHistoryListAsync(long cid, string month, CancellationToken cancellationToken)
        {
            var url = $"http://api.bilibili.com/x/v2/dm/history/index?type=1&oid={cid}&month={month}";
            return await httpClient.GetStringAsync(url, cancellationToken).ConfigureAwait(false);
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
            var startTime = DateTime.Now;
            lock (_lock)
            {
                var ts = DateTime.Now - lastRequestTime;
                var diff = (int)(200 - ts.TotalMilliseconds);
                if (diff > 0)
                {
                    Thread.Sleep(diff);
                }
                lastRequestTime = DateTime.Now;
            }
            var endTime = DateTime.Now;
            var tt = (endTime - startTime).TotalMilliseconds;
            Console.WriteLine(tt);
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
}
