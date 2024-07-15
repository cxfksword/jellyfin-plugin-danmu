using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Emby.Plugin.Danmu.Configuration;
using Emby.Plugin.Danmu.Core.Extensions;
using Emby.Plugin.Danmu.Core.Singleton;
using Emby.Plugin.Danmu.Scraper.Dandan.Entity;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Caching.Memory;

namespace Emby.Plugin.Danmu.Scraper.Dandan
{
    public class DandanApi : AbstractApi
    {
        private static readonly object _lock = new object();
        private DateTime lastRequestTime = DateTime.Now.AddDays(-1);
        private readonly ILogger _logger;
        private readonly IJsonSerializer _jsonSerializer;


        public DandanOption Config
        {
            get { return Plugin.Instance?.Configuration.Dandan ?? new DandanOption(); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DandanApi"/> class.
        /// </summary>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        public DandanApi(ILogManager logManager, IJsonSerializer jsonSerializer, IHttpClient httpClient)
            : base(logManager.GetLogger("DandanApi"), httpClient)
        {
            _logger = logManager.getDefaultLogger(GetType().ToString());
            this._jsonSerializer = jsonSerializer;
            // IHttpClient client = ServiceRegistrator.GetByType<IHttpClient>();
            // httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }


        public async Task<List<Anime>> SearchAsync(string keyword, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(keyword))
            {
                return new List<Anime>();
            }

            var cacheKey = $"search_{keyword}";
            var expiredOption = new MemoryCacheEntryOptions()
                { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) };
            if (_memoryCache.TryGetValue<List<Anime>>(cacheKey, out var searchResult))
            {
                return searchResult;
            }

            this.LimitRequestFrequently();

            keyword = HttpUtility.UrlEncode(keyword);
            var url = $"https://api.dandanplay.net/api/v2/search/anime?keyword={keyword}";
            var response = await httpClient.GetResponse(new HttpRequestOptions
            {
                //Url = $"http://sub.xmp.sandai.net:8000/subxl/{cid}.json",
                Url = url,
                UserAgent = $"{HTTP_USER_AGENT}",
                TimeoutMs = 30000,
                AcceptHeader = "application/json",
            }).ConfigureAwait(false);

            // _logger.Info("res = {0}", response.ToString());
            // _logger.Info("{0} Search | Response -> {1}", url, _jsonSerializer.SerializeToString(response));
            
            if (response.StatusCode != HttpStatusCode.OK)
            { 
                return new List<Anime>();
            }

            // var result = await response.Content.ReadFromJsonAsync<SearchResult>();
            SearchResult result = _jsonSerializer.DeserializeFromStream<SearchResult>(response.Content);
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
            var expiredOption = new MemoryCacheEntryOptions()
                { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) };
            if (_memoryCache.TryGetValue<Anime?>(cacheKey, out var anime))
            {
                return anime;
            }

            var url = $"https://api.dandanplay.net/api/v2/bangumi/{animeId}";
            var response = await httpClient.GetResponse(new HttpRequestOptions
            {
                //Url = $"http://sub.xmp.sandai.net:8000/subxl/{cid}.json",
                Url = url,
                UserAgent = $"{HTTP_USER_AGENT}",
                TimeoutMs = 30000,
                AcceptHeader = "application/json",
            }).ConfigureAwait(false);
            // var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
            // response.EnsureSuccessStatusCode();
                        
            if (response.StatusCode != HttpStatusCode.OK)
            { 
                return null;
            }
            
            AnimeResult result = _jsonSerializer.DeserializeFromStream<AnimeResult>(response.Content);
            // var result = await response.Content.ReadFromJsonAsync<AnimeResult>(cancellationToken)
            //     .ConfigureAwait(false);
            if (result != null && result.Success && result.Bangumi != null)
            {
                // 过滤掉特典剧集，episodeNumber为S1/S2.。。
                anime = result.Bangumi;
                if (anime.Episodes != null)
                {
                    anime.Episodes = anime.Episodes.Where(x =>
                    {
                        bool success = int.TryParse(x.EpisodeNumber, out int parsedNumber);
                        return success && parsedNumber > 0;
                    }).ToList();
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
            var result = await httpClient.GetSelfResultAsync<CommentResult>(GetDefaultHttpRequestOptions(url)).ConfigureAwait(false);
            
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
                this._logger.Debug("请求太频繁，等待{0}毫秒后继续执行...", diff);
                Thread.Sleep(diff);
            }
        }
    }
}