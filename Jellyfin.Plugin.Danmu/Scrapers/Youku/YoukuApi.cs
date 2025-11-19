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
using Jellyfin.Plugin.Danmu.Scrapers.Youku.Entity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using RateLimiter;

namespace Jellyfin.Plugin.Danmu.Scrapers.Youku;

public class YoukuApi : AbstractApi
{
    private static readonly Regex yearReg = new Regex(@"[12][890][0-9][0-9]", RegexOptions.Compiled);
    private static readonly Regex unusedWordsReg = new Regex(@"\[.+?\]|\(.+?\)|【.+?】", RegexOptions.Compiled);


    private TimeLimiter _timeConstraint = TimeLimiter.GetFromMaxCountByInterval(1, TimeSpan.FromMilliseconds(1000));
    private TimeLimiter _delayExecuteConstraint = TimeLimiter.GetFromMaxCountByInterval(1, TimeSpan.FromMilliseconds(100));
    private TimeLimiter _delayShortExecuteConstraint = TimeLimiter.GetFromMaxCountByInterval(1, TimeSpan.FromMilliseconds(10));

    // 并行请求配置
    private const int DefaultParallelCount = 3;

    protected string _cna = string.Empty;
    protected string _token = string.Empty;
    protected string _tokenEnc = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="YoukuApi"/> class.
    /// </summary>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    public YoukuApi(ILoggerFactory loggerFactory)
        : base(loggerFactory.CreateLogger<YoukuApi>())
    {
    }


    public async Task<List<YoukuVideo>> SearchAsync(string keyword, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(keyword))
        {
            return new List<YoukuVideo>();
        }

        var cacheKey = $"search_{keyword}";
        var expiredOption = new MemoryCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) };
        if (this._memoryCache.TryGetValue<List<YoukuVideo>>(cacheKey, out var cacheValue))
        {
            return cacheValue;
        }

        await this.LimitRequestFrequently();

        keyword = HttpUtility.UrlEncode(keyword);
        var ua = HttpUtility.UrlEncode(AbstractApi.HTTP_USER_AGENT);
        var url = $"https://search.youku.com/api/search?keyword={keyword}&userAgent={ua}&site=1&categories=0&ftype=0&ob=0&pg=1";
        using var response = await this.httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var result = new List<YoukuVideo>();
        var searchResult = await response.Content.ReadFromJsonAsync<YoukuSearchResult>(this._jsonOptions, cancellationToken).ConfigureAwait(false);
        if (searchResult != null && searchResult.PageComponentList != null)
        {
            foreach (YoukuSearchComponent component in searchResult.PageComponentList)
            {
                if (component.CommonData == null
                || component.CommonData.TitleDTO == null
                || component.CommonData.HasYouku != 1
                || component.CommonData.IsYouku != 1)
                {
                    continue;
                }

                if (component.CommonData.TitleDTO.DisplayName.Contains("中配版")
                ||  component.CommonData.TitleDTO.DisplayName.Contains("抢先看")
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
                    Year = year > 0 ? year : null,
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
        var expiredOption = new MemoryCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) };
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
                var pagerVideo = await this.GetVideoEpisodesAsync(id, pn, pageSize, cancellationToken).ConfigureAwait(false);
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
    private async Task<YoukuVideo?> GetVideoEpisodesAsync(string id, int page, int pageSize, CancellationToken cancellationToken)
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
        var url = $"https://openapi.youku.com/v2/shows/videos.json?client_id=53e6cc67237fc59a&package=com.huawei.hwvplayer.youku&ext=show&show_id={id}&page={page}&count={pageSize}";
        using var response = await this.httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<YoukuVideo>(this._jsonOptions, cancellationToken).ConfigureAwait(false);
        if (result != null)
        {
            // 过滤掉彩蛋
            if (result.Videos != null && result.Videos.Count > 0)
            {
                var filterList = result.Videos.Where(v => !v.Title.Contains("彩蛋"));

                // 综艺会包含各种版本和花絮，这里过滤掉
                if (result.Videos[0].Category == "综艺" || result.Videos[0].Category == "娱乐")
                {
                    var validPrefixs = new string[] {"上", "中", "下"};
                    filterList = filterList.Where(v => !v.Title.Contains("：") || validPrefixs.Contains(v.Title.Split("：").First().Trim()))
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
        var expiredOption = new MemoryCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) };
        if (this._memoryCache.TryGetValue<YoukuEpisode?>(cacheKey, out var episode))
        {
            return episode;
        }

        // 文档：https://cloud.youku.com/docs?id=46
        var url = $"https://openapi.youku.com/v2/videos/show_basic.json?client_id=53e6cc67237fc59a&package=com.huawei.hwvplayer.youku&video_id={vid}";
        using var response = await this.httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<YoukuEpisode>(this._jsonOptions, cancellationToken).ConfigureAwait(false);
        if (result != null)
        {
            this._memoryCache.Set<YoukuEpisode?>(cacheKey, result, expiredOption);
            return result;
        }

        this._memoryCache.Set<YoukuEpisode?>(cacheKey, null, expiredOption);
        return null;
    }

    public async Task<List<YoukuComment>> GetDanmuContentAsync(string vid, CancellationToken cancellationToken, bool isParallel = false)
    {
        return await this.GetDanmuContentAsync(vid, isParallel, DefaultParallelCount, cancellationToken).ConfigureAwait(false);
    }

    public async Task<List<YoukuComment>> GetDanmuContentAsync(string vid, bool isParallel, int parallelCount, CancellationToken cancellationToken)
    {
        var danmuList = new List<YoukuComment>();
        if (string.IsNullOrEmpty(vid))
        {
            return danmuList;
        }

        if (parallelCount <= 0)
        {
            parallelCount = DefaultParallelCount;
        }

        await this.EnsureTokenCookie(cancellationToken);

        var episode = await this.GetEpisodeAsync(vid, cancellationToken);
        if (episode == null)
        {
            return danmuList;
        }

        var totalMat = episode.TotalMat;

        if (isParallel)
        {
            // 并行执行
            var tasks = new List<Task<List<YoukuComment>>>();
            var semaphore = new SemaphoreSlim(parallelCount, parallelCount);

            for (int mat = 0; mat < totalMat; mat++)
            {
                var currentMat = mat;
                await semaphore.WaitAsync(cancellationToken);

                var task = Task.Run(async () =>
                {
                    try
                    {
                        await this._delayShortExecuteConstraint;
                        return await this.GetDanmuContentByMatAsync(vid, currentMat, cancellationToken).ConfigureAwait(false);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }, cancellationToken);

                tasks.Add(task);
            }

            var results = await Task.WhenAll(tasks).ConfigureAwait(false);
            foreach (var comments in results)
            {
                danmuList.AddRange(comments);
            }
        }
        else
        {
            // 串行执行
            for (int mat = 0; mat < totalMat; mat++)
            {
                var comments = await this.GetDanmuContentByMatAsync(vid, mat, cancellationToken);
                danmuList.AddRange(comments);

                // 等待一段时间避免api请求太快
                await this._delayShortExecuteConstraint;
            }
        }

        return danmuList;
    }

    // mat从0开始，视频分钟数
    public async Task<List<YoukuComment>> GetDanmuContentByMatAsync(string vid, int mat, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(vid))
        {
            return new List<YoukuComment>();
        }

        await this.EnsureTokenCookie(cancellationToken);


        var ctime = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
        var msg = new Dictionary<string, object>() {
            {"pid", 0},
            {"ctype", 10004},
            {"sver", "3.1.0"},
            {"cver", "v1.0"},
            {"ctime" , ctime},
            {"guid", this._cna},
            {"vid", vid},
            {"mat", mat},
            {"mcount", 1},
            {"type", 1}
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
        var formContent = new FormUrlEncodedContent(new[]{
                new KeyValuePair<string, string>("data", data)
        });
        using (var requestMessage = new HttpRequestMessage(HttpMethod.
        Post, builder.Uri.ToString())
        { Content = formContent })
        {
            requestMessage.Headers.Add("Referer", "https://v.youku.com");

            response = await this.httpClient.SendAsync(requestMessage);
        }
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<YoukuRpcResult>(this._jsonOptions, cancellationToken).ConfigureAwait(false);
        if (result != null && !string.IsNullOrEmpty(result.Data.Result))
        {
            var commentResult = JsonSerializer.Deserialize<YoukuCommentResult>(result.Data.Result);
            if (commentResult != null && commentResult.Data != null)
            {
                // 每段有60秒弹幕，为避免弹幕太大，从中间隔抽取最大60秒200条弹幕
                return commentResult.Data.Result.ExtractToNumber(200).ToList();
            }
        }

        return new List<YoukuComment>();
    }

    protected async Task LimitRequestFrequently()
    {
        await this._timeConstraint;
    }

    protected async Task EnsureTokenCookie(CancellationToken cancellationToken)
    {
        var cookies = this._cookieContainer.GetCookies(new Uri("https://mmstat.com", UriKind.Absolute));
        var cookie = cookies.FirstOrDefault(x => x.Name == "cna");
        if (cookie == null)
        {
            var url = "https://log.mmstat.com/eg.js";
            using var response = await this.httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            // 重新读取最新
            cookies = this._cookieContainer.GetCookies(new Uri("https://mmstat.com", UriKind.Absolute));
            cookie = cookies.FirstOrDefault(x => x.Name == "cna");
        }
        if (cookie != null)
        {
            this._cna = cookie.Value;
        }


        cookies = this._cookieContainer.GetCookies(new Uri("https://youku.com", UriKind.Absolute));
        var tokenCookie = cookies.FirstOrDefault(x => x.Name == "_m_h5_tk");
        var tokenEncCookie = cookies.FirstOrDefault(x => x.Name == "_m_h5_tk_enc");
        if (tokenCookie == null || tokenEncCookie == null)
        {
            var url = "https://acs.youku.com/h5/mtop.com.youku.aplatform.weakget/1.0/?jsv=2.5.1&appKey=24679788";
            using var response = await this.httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            // 重新读取最新
            cookies = this._cookieContainer.GetCookies(new Uri("https://youku.com", UriKind.Absolute));
            tokenCookie = cookies.FirstOrDefault(x => x.Name == "_m_h5_tk");
            tokenEncCookie = cookies.FirstOrDefault(x => x.Name == "_m_h5_tk_enc");
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
        return string.Join('&', arr).ToMD5().ToLower();
    }
}

