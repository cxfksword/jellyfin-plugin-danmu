using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Emby.Plugin.Danmu.Core.Extensions;
using Emby.Plugin.Danmu.Core.Singleton;
using Emby.Plugin.Danmu.Scraper.Youku.Entity;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Logging;
using Microsoft.Extensions.Caching.Memory;

namespace Emby.Plugin.Danmu.Scraper.Youku
{
    public class YoukuApi : AbstractApi
    {
        private static readonly Regex yearReg = new Regex(@"[12][890][0-9][0-9]", RegexOptions.Compiled);
        private static readonly Regex unusedWordsReg = new Regex(@"\[.+?\]|\(.+?\)|【.+?】", RegexOptions.Compiled);


        protected string _cna = string.Empty;
        protected string _token = string.Empty;
        protected string _tokenEnc = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="YoukuApi"/> class.
        /// </summary>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        public YoukuApi(ILogManager logManager, IHttpClient httpClient)
            : base(logManager.getDefaultLogger("YoukuApi"), httpClient)
        {
        }

        public async Task<List<YoukuVideo>> SearchAsync(string keyword, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(keyword))
            {
                return new List<YoukuVideo>();
            }

            var cacheKey = $"search_{keyword}";
            var expiredOption = new MemoryCacheEntryOptions()
                { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) };
            if (this._memoryCache.TryGetValue<List<YoukuVideo>>(cacheKey, out var cacheValue))
            {
                return cacheValue;
            }

            await this.LimitRequestFrequently();

            keyword = HttpUtility.UrlEncode(keyword);
            var ua = HttpUtility.UrlEncode(AbstractApi.HTTP_USER_AGENT);
            var url =
                $"https://search.youku.com/api/search?keyword={keyword}&userAgent={ua}&site=1&categories=0&ftype=0&ob=0&pg=1";

            var result = new List<YoukuVideo>();
            var searchResult =
                await httpClient.GetSelfResultAsyncWithError<YoukuSearchResult>(
                    GetDefaultHttpRequestOptions(url, null, cancellationToken), null, "GET");
            if (searchResult != null && searchResult.PageComponentList != null)
            {
                foreach (YoukuSearchComponent component in searchResult.PageComponentList)
                {
                    if (component.CommonData == null
                        || component.CommonData.TitleDTO == null
                        || (component.CommonData.HasYouku != 1 && component.CommonData.IsYouku != 1))
                    {
                        continue;
                    }

                    if (component.CommonData.TitleDTO.DisplayName.Contains("中配版")
                        || component.CommonData.TitleDTO.DisplayName.Contains("抢先看")
                        || component.CommonData.TitleDTO.DisplayName.Contains("非正片")
                        || component.CommonData.TitleDTO.DisplayName.Contains("解读")
                        || component.CommonData.TitleDTO.DisplayName.Contains("揭秘")
                        || component.CommonData.TitleDTO.DisplayName.Contains("赏析")
                        || component.CommonData.TitleDTO.DisplayName.Contains("《"))
                    {
                        continue;
                    }

                    var year = yearReg.FirstMatch(component.CommonData.Feature).ToInt();
                    result.Add(new YoukuVideo()
                    {
                        ID = component.CommonData.ShowId,
                        Type = component.CommonData.Feature.Contains("电影") ? "movie" : "tv",
                        Year = year > 0 ? year : (int?)null,
                        Title = unusedWordsReg.Replace(component.CommonData.TitleDTO.DisplayName, ""),
                        Total = component.CommonData.EpisodeTotal
                    });
                }
            }

            this._memoryCache.Set<List<YoukuVideo>>(cacheKey, result, expiredOption);
            return result;
        }

        /// <summary>
        /// 获取影片的详细信息
        /// </summary>
        public async Task<YoukuVideo?> GetVideoAsync(string id, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            await this.LimitRequestFrequently();

            var cacheKey = $"video_{id}";
            var expiredOption = new MemoryCacheEntryOptions()
                { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) };
            if (this._memoryCache.TryGetValue<YoukuVideo?>(cacheKey, out var video))
            {
                return video;
            }

            var pageSize = 20;
            video = await this.GetVideoEpisodesAsync(id, 1, pageSize, cancellationToken).ConfigureAwait(false);
            if (video != null)
            {
                var pageCount = (video.Total / pageSize) + (video.Total % pageSize > 0 ? 1 : 0);
                for (int pn = 2; pn <= pageCount; pn++)
                {
                    var pagerVideo = await this.GetVideoEpisodesAsync(id, pn, pageSize, cancellationToken)
                        .ConfigureAwait(false);
                    if (pagerVideo != null)
                    {
                        video.Videos.AddRange(pagerVideo.Videos);
                    }
                }

                // 可能过滤了彩蛋，更新总数
                video.Total = video.Videos.Count;
            }

            this._memoryCache.Set<YoukuVideo?>(cacheKey, video, expiredOption);
            return video;
        }

        /// <summary>
        /// 获取影片剧集分页信息
        /// </summary>
        private async Task<YoukuVideo?> GetVideoEpisodesAsync(string id, int page, int pageSize,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            if (page <= 0)
            {
                page = 1;
            }

            if (pageSize <= 0)
            {
                pageSize = 20;
            }

            // 接口文档：https://cloud.youku.com/docs?id=64
            // 获取影片信息：https://openapi.youku.com/v2/shows/show.json?client_id=53e6cc67237fc59a&package=com.huawei.hwvplayer.youku&show_id=0b39c5b6569311e5b2ad
            // 获取影片剧集信息：https://openapi.youku.com/v2/shows/videos.json?client_id=53e6cc67237fc59a&package=com.huawei.hwvplayer.youku&ext=show&show_id=deea7e54c2594c489bfd
            var url =
                $"https://openapi.youku.com/v2/shows/videos.json?client_id=53e6cc67237fc59a&package=com.huawei.hwvplayer.youku&ext=show&show_id={id}&page={page}&count={pageSize}";
            var result = await httpClient.GetSelfResultAsyncWithError<YoukuVideo>(GetDefaultHttpRequestOptions(url))
                .ConfigureAwait(false);
            // var result = await response.Content.ReadFromJsonAsync<YoukuVideo>(this._jsonOptions, cancellationToken).ConfigureAwait(false);
            if (result != null)
            {
                // 过滤掉彩蛋
                if (result.Videos != null && result.Videos.Count > 0)
                {
                    var filterList = result.Videos.Where(v => !v.Title.Contains("彩蛋"));

                    // 综艺会包含各种版本和花絮，这里过滤掉
                    if (result.Videos[0].Category == "综艺" || result.Videos[0].Category == "娱乐")
                    {
                        var validPrefixs = new string[] { "上", "中", "下" };
                        filterList = filterList.Where(v =>
                                !v.Title.Contains("：") || validPrefixs.Contains(v.Title.Split('：').First().Trim()))
                            .Where(v => v.Category != "娱乐");
                    }

                    result.Videos = filterList.ToList();
                }

                return result;
            }

            return null;
        }


        /// <summary>
        /// 获取单个剧集信息
        /// </summary>
        public async Task<YoukuEpisode?> GetEpisodeAsync(string vid, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(vid))
            {
                return null;
            }

            var cacheKey = $"episode_{vid}";
            var expiredOption = new MemoryCacheEntryOptions()
                { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) };
            if (this._memoryCache.TryGetValue<YoukuEpisode?>(cacheKey, out var episode))
            {
                return episode;
            }

            // 文档：https://cloud.youku.com/docs?id=46
            var url =
                $"https://openapi.youku.com/v2/videos/show_basic.json?client_id=53e6cc67237fc59a&package=com.huawei.hwvplayer.youku&video_id={vid}";
            var result = await httpClient
                .GetSelfResultAsyncWithError<YoukuEpisode>(GetDefaultHttpRequestOptions(url, null, cancellationToken))
                .ConfigureAwait(false);
            // var result = await response.Content.ReadFromJsonAsync<YoukuEpisode>(this._jsonOptions, cancellationToken).ConfigureAwait(false);
            if (result != null)
            {
                this._memoryCache.Set<YoukuEpisode?>(cacheKey, result, expiredOption);
                return result;
            }

            this._memoryCache.Set<YoukuEpisode?>(cacheKey, null, expiredOption);
            return null;
        }

        public async Task<List<YoukuComment>> GetDanmuContentAsync(string vid, CancellationToken cancellationToken)
        {
            var danmuList = new List<YoukuComment>();
            if (string.IsNullOrEmpty(vid))
            {
                return danmuList;
            }

            await this.EnsureTokenCookie(cancellationToken);


            var episode = await this.GetEpisodeAsync(vid, cancellationToken);
            if (episode == null)
            {
                return danmuList;
            }

            var totalMat = episode.TotalMat;
            for (int mat = 0; mat < totalMat; mat++)
            {
                var comments = await this.GetDanmuContentByMatAsync(vid, mat, cancellationToken);
                danmuList.AddRange(comments);

                // 等待一段时间避免api请求太快
                Thread.Sleep(100);
                // await this._delayExecuteConstraint;
            }

            return danmuList;
        }

        // mat从0开始，视频分钟数
        public async Task<List<YoukuComment>> GetDanmuContentByMatAsync(string vid, int mat,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(vid))
            {
                return new List<YoukuComment>();
            }

            await this.EnsureTokenCookie(cancellationToken);

            var ctime = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
            var msg = new Dictionary<string, object>()
            {
                { "pid", 0 },
                { "ctype", 10004 },
                { "sver", "3.1.0" },
                { "cver", "v1.0" },
                { "ctime", ctime },
                { "guid", this._cna },
                { "vid", vid },
                { "mat", mat },
                { "mcount", 1 },
                { "type", 1 }
            };

            // 需key按字母排序
            var msgOrdered = msg.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value).ToJson();
            var msgEnc = Convert.ToBase64String(Encoding.UTF8.GetBytes(msgOrdered));
            var sign = this.generateMsgSign(msgEnc);
            msg.Add("msg", msgEnc);
            msg.Add("sign", sign);


            var appKey = "24679788";
            var data = msg.ToJson();
            var t = Convert.ToString(new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds());
            var param = HttpUtility.ParseQueryString(string.Empty);
            param["jsv"] = "2.7.0";
            param["appKey"] = appKey;
            param["t"] = t;
            param["sign"] = this.generateTokenSign(t, appKey, data);
            param["api"] = "mopen.youku.danmu.list";
            param["v"] = "1.0";
            param["type"] = "originaljson";
            param["dataType"] = "jsonp";
            param["timeout"] = "20000";
            param["jsonpIncPrefix"] = "utility";

            var builder = new UriBuilder("https://acs.youku.com/h5/mopen.youku.danmu.list/1.0/");
            builder.Query = param.ToString();
            HttpResponseMessage response;
            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("data", data)
            });

            var postData = new Dictionary<string, string>();
            postData["data"] = data;
            HttpRequestOptions defaultHttpRequestOptions = GetDefaultHttpRequestOptions(builder.Uri.ToString());
            defaultHttpRequestOptions.RequestHeaders.Add("Referer", "https://v.youku.com");
            defaultHttpRequestOptions.SetPostData(postData);

            var result = await httpClient.GetSelfResultAsyncWithError<YoukuRpcResult>(defaultHttpRequestOptions)
                .ConfigureAwait(false);
            // var result = await response.Content.ReadFromJsonAsync<YoukuRpcResult>(this._jsonOptions, cancellationToken).ConfigureAwait(false);
            if (result != null && !string.IsNullOrEmpty(result.Data.Result))
            {
                var commentResult = SingletonManager.JsonSerializer.DeserializeFromString<YoukuCommentResult>(result.Data.Result);
                if (commentResult != null && commentResult.Data != null)
                {
                    // 每段有60秒弹幕，为避免弹幕太大，从中间隔抽取最大60秒200条弹幕
                    return commentResult.Data.Result.ExtractToNumber(200).ToList();
                }
            }

            return new List<YoukuComment>();
        }

        protected async Task EnsureTokenCookie(CancellationToken cancellationToken)
        {
            Uri mmstatUrl = new Uri("https://mmstat.com", UriKind.Absolute);
            var cookies = this._cookieContainer.GetCookies(mmstatUrl);

            var cookie = cookies
                .Cast<Cookie>()
                .FirstOrDefault(x => x.Name == "cna");

            if (cookie == null)
            {
                var url = "https://log.mmstat.com/eg.js";
                HttpRequestOptions requestOptions = GetDefaultHttpRequestOptions(url, null, cancellationToken);
                var response = await this.httpClient.GetAsync(requestOptions).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                
                if (response.Headers.TryGetValue("Set-Cookie", out string setCookieHeaders))
                {
                    AddCookies(mmstatUrl, setCookieHeaders);
                }
                
                // 重新读取最新
                cookies = this._cookieContainer.GetCookies(mmstatUrl);
                cookie = cookies
                    .Cast<Cookie>()
                    .FirstOrDefault(x => x.Name == "cna");
                _logger.Info("cookie已失效，重新获取cookies, mmstat.cookies={0}", cookies.ToJson());
            }

            if (cookie != null)
            {
                this._cna = cookie.Value;
            }

            var youkuUrl = new Uri("https://youku.com", UriKind.Absolute);
            cookies = this._cookieContainer.GetCookies(youkuUrl);
            
            var tokenCookie = cookies.Cast<Cookie>().FirstOrDefault(x => x.Name == "_m_h5_tk");
            var tokenEncCookie = cookies.Cast<Cookie>().FirstOrDefault(x => x.Name == "_m_h5_tk_enc");
            
            // _logger.Info("tokenCookie={0}, tokenEncCookie={1}", tokenCookie.ToJson(), tokenEncCookie.ToJson());
            if (tokenCookie == null || tokenEncCookie == null)
            {
                var url = "https://acs.youku.com/h5/mtop.com.youku.aplatform.weakget/1.0/?jsv=2.5.1&appKey=24679788";
                var defaultHttpRequestOptions = GetDefaultHttpRequestOptions(url, null, cancellationToken);
                var response = await this.httpClient.GetAsync(defaultHttpRequestOptions).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                if (response.Headers.TryGetValue("Set-Cookie", out string setCookieHeaders))
                {
                    // 多个cookie会被合并
                    // _m_h5_tk=ad2f7c342e493a94314704225caa2c28_1721027698052;Path=/;Domain=youku.com;Max-Age=86400;SameSite=None;Secure _m_h5_tk_enc=5a942d7a7d2927de4cf44d296c01cf21;Path=/;Domain=youku.com;Max-Age=86400;SameSite=None;Secure
                    AddCookies(youkuUrl, setCookieHeaders, ' ');
                }

                // 重新读取最新
                cookies = this._cookieContainer.GetCookies(youkuUrl);
                tokenCookie = cookies.Cast<Cookie>().FirstOrDefault(x => x.Name == "_m_h5_tk");
                tokenEncCookie = cookies.Cast<Cookie>().FirstOrDefault(x => x.Name == "_m_h5_tk_enc");
                _logger.Info("cookie已失效，重新获取cookies, youkuUrl.cookies={0}", cookies.ToJson());
            }

            if (tokenCookie != null)
            {
                this._token = tokenCookie.Value;
            }

            if (tokenEncCookie != null)
            {
                this._tokenEnc = tokenEncCookie.Value;
            }
        }


        protected string generateMsgSign(string msgEnc)
        {
            return (msgEnc + "MkmC9SoIw6xCkSKHhJ7b5D2r51kBiREr").ToMD5().ToLower();
        }

        protected string generateTokenSign(string t, string appKey, string data)
        {
            var arr = new string[] { this._token.Substring(0, 32), t, appKey, data };
            return string.Join("&", arr).ToMD5().ToLower();
        }
    }
}