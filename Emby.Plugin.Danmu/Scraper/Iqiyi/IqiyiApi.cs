using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Serialization;
using Emby.Plugin.Danmu.Core;
using Emby.Plugin.Danmu.Core.Extensions;
using Emby.Plugin.Danmu.Core.Singleton;
using Emby.Plugin.Danmu.Scraper.Iqiyi.Entity;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Logging;
using Microsoft.Extensions.Caching.Memory;

namespace Emby.Plugin.Danmu.Scraper.Iqiyi
{
    public class IqiyiApi : AbstractApi
    {
        private static readonly Regex regVideoInfo = new Regex(@"pageProps"":\{""videoInfo"":(\{.+?\}),""featureInfo", RegexOptions.Compiled);
        private static readonly Regex regAlbumInfo = new Regex(@"""albumInfo"":(\{.+?\}),", RegexOptions.Compiled);
        
        // private TimeLimiter _timeConstraint = TimeLimiter.GetFromMaxCountByInterval(1, TimeSpan.FromMilliseconds(1000));
        // private TimeLimiter _delayExecuteConstraint = TimeLimiter.GetFromMaxCountByInterval(1, TimeSpan.FromMilliseconds(100));
        
        public IqiyiApi(ILogManager logManager, IHttpClient httpClient)
            : base(logManager.getDefaultLogger(typeof(IqiyiApi).ToString()), httpClient)
        {
            
        }
        
    public async Task<List<IqiyiSearchAlbumInfo>> SearchAsync(string keyword, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(keyword))
        {
            return new List<IqiyiSearchAlbumInfo>();
        }

        var cacheKey = $"search_{keyword}";
        var expiredOption = new MemoryCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) };
        if (!SingletonManager.IsDebug && 
            _memoryCache.TryGetValue<List<IqiyiSearchAlbumInfo>>(cacheKey, out var cacheValue))
        {
            return cacheValue;
        }

        await this.LimitRequestFrequently();

        keyword = HttpUtility.UrlEncode(keyword);
        var url = $"https://search.video.iqiyi.com/o?if=html5&key={keyword}&pageNum=1&pageSize=20";

        var result = new List<IqiyiSearchAlbumInfo>();
        var searchResult = await httpClient.GetSelfResultAsync<IqiyiSearchResult>(GetDefaultHttpRequestOptions(url), null).ConfigureAwait(false);
        if (searchResult != null && searchResult.Data != null)
        {
            result = searchResult.Data.DocInfos
                .Where(x => x.Score > 0.7)
                .Select(x => x.AlbumDocInfo)
                .Where(x => !string.IsNullOrEmpty(x.Link) && x.Link.Contains("iqiyi.com") && x.SiteId == "iqiyi" && x.VideoDocType == 1 && !x.Channel.Contains("原创") && !x.Channel.Contains("教育"))
                .ToList();
        }

        _memoryCache.Set<List<IqiyiSearchAlbumInfo>>(cacheKey, result, expiredOption);
        return result;
    }

    public async Task<IqiyiHtmlVideoInfo?> GetVideoAsync(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        var cacheKey = $"video_{id}";
        var expiredOption = new MemoryCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) };
        if (!SingletonManager.IsDebug && _memoryCache.TryGetValue<IqiyiHtmlVideoInfo?>(cacheKey, out var video))
        {
            return video;
        }

        // 获取电视剧信息(aid)：https://pcw-api.iqiyi.com/album/album/baseinfo/5328486914190101
        // 获取电视剧剧集信息(综艺不适用)(aid)：https://pcw-api.iqiyi.com/albums/album/avlistinfo?aid=5328486914190101&page=1&size=10
        // 获取电视剧剧集信息(综艺不适用)(aid)：https://pub.m.iqiyi.com/h5/main/videoList/album/?albumId=5328486914190101&size=39&page=1&needPrevue=true&needVipPrevue=false
        var videoInfo = await GetVideoBaseAsync(id, cancellationToken).ConfigureAwait(false);
        if (videoInfo != null)
        {
            if (videoInfo.channelId == 6)
            { // 综艺需要特殊处理
                videoInfo.Epsodelist = await this.GetZongyiEpisodesAsync($"{videoInfo.AlbumId}", cancellationToken).ConfigureAwait(false);
            }
            else if (videoInfo.channelId == 1)
            { // 电影
                var duration = new TimeSpan(0, 0, videoInfo.Duration);
                videoInfo.Epsodelist = new List<IqiyiEpisode>() {
                    new IqiyiEpisode() {TvId = videoInfo.TvId, Order = 1, Name = videoInfo.VideoName, Duration = duration.ToString(@"hh\:mm\:ss"), PlayUrl = videoInfo.VideoUrl}
                };
            }
            else
            { // 电视剧需要再获取剧集信息
                videoInfo.Epsodelist = await this.GetEpisodesAsync($"{videoInfo.AlbumId}", videoInfo.VideoCount, cancellationToken).ConfigureAwait(false);
            }

            _memoryCache.Set<IqiyiHtmlVideoInfo?>(cacheKey, videoInfo, expiredOption);
            return videoInfo;
        }

        _memoryCache.Set<IqiyiHtmlVideoInfo?>(cacheKey, null, expiredOption);
        return null;
    }

    public async Task<IqiyiHtmlVideoInfo?> GetVideoBaseAsync(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        var cacheKey = $"video_base_{id}";
        var expiredOption = new MemoryCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) };
        if (!SingletonManager.IsDebug && _memoryCache.TryGetValue<IqiyiHtmlVideoInfo?>(cacheKey, out var video))
        {
            return video;
        }

        await this.LimitRequestFrequently();

        var url = $"https://www.iqiyi.com/v_{id}.html";
        var videoInfo = await httpClient.GetSelfResultAsync<IqiyiHtmlVideoInfo>(GetDefaultHttpRequestOptions(url, null, cancellationToken), response =>
        {
            
            // 确保响应状态码为成功
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                // 读取响应流
                using (var responseStream = response.Content)
                using (var reader = new StreamReader(responseStream, Encoding.UTF8))
                {
                    var htmlResult = reader.ReadToEnd();
                    var videoJson = regVideoInfo.FirstMatchGroup(htmlResult);
                    return videoJson;
                }
            }
            else
            {
                // 处理不成功的响应
                throw new InvalidOperationException($"请求失败，HTTP 状态码：{response.StatusCode}");
            }
            
            
            // var body = response.Content.ReadFromJsonAsync<string>().ConfigureAwait(false);
            //
            // string result = body.GetAwaiter().GetResult();
            // var videoJson = regVideoInfo.FirstMatchGroup(result);
            //
            // _logger.Info("解析结果数据 videoJson={0}", videoJson);
            // return videoJson;
        });
        
        // var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        // response.EnsureSuccessStatusCode();

        // var body = await response.Content.ReadAsStringAsync();
        // var videoJson = regVideoInfo.FirstMatchGroup(body);
        // var videoInfo = videoJson.FromJson<IqiyiHtmlVideoInfo>();
        if (videoInfo != null)
        {
            this._memoryCache.Set(cacheKey, videoInfo, expiredOption);
            return videoInfo;
        }
        return null;
    }

    /// <summary>
    /// 获取电视剧剧集列表(综艺不适用)
    /// </summary>
    public async Task<List<IqiyiEpisode>> GetEpisodesAsync(string albumId, int size, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(albumId))
        {
            return new List<IqiyiEpisode>();
        }

        var url = $"https://pcw-api.iqiyi.com/albums/album/avlistinfo?aid={albumId}&page=1&size={size}";
        // var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        // response.EnsureSuccessStatusCode();

        var albumResult = await httpClient.GetSelfResultAsyncWithError<IqiyiVideoResult>(GetDefaultHttpRequestOptions(url), null).ConfigureAwait(false);
        if (albumResult != null && albumResult.Data != null && albumResult.Data.Epsodelist != null)
        {
            return albumResult.Data.Epsodelist;
        }

        return new List<IqiyiEpisode>();
    }

    /// <summary>
    /// 获取综艺剧集列表
    /// </summary>
    public async Task<List<IqiyiEpisode>> GetZongyiEpisodesAsync(string albumId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(albumId))
        {
            return new List<IqiyiEpisode>();
        }

        var url = $"https://pcw-api.iqiyi.com/album/album/baseinfo/{albumId}";
        var albumResult = await httpClient.GetSelfResultAsyncWithError<IqiyiAlbumResult>(GetDefaultHttpRequestOptions(url), null).ConfigureAwait(false);
        if (albumResult != null && albumResult.Data != null && albumResult.Data.FirstVideo != null && albumResult.Data.LatestVideo != null)
        {
            var startDate = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddMilliseconds(albumResult.Data.FirstVideo.publishTime).ToLocalTime();
            var endDate = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddMilliseconds(albumResult.Data.LatestVideo.publishTime).ToLocalTime();
            // 超过一年的太大直接不处理
            var totalDays = (endDate - startDate).TotalDays;
            if (totalDays > 365)
            {
                return new List<IqiyiEpisode>();
            }

            var list = new List<IqiyiVideoListInfo>();
            for (var begin = startDate; begin.Month <= endDate.Month; begin = begin.AddMonths(1))
            {
                var year = begin.Year;
                var month = begin.ToString("MM");
                url = $"https://pub.m.iqiyi.com/h5/main/videoList/source/month/?sourceId={albumId}&year={year}&month={month}";

                var videoListResult = await httpClient.GetSelfResultAsyncWithError<IqiyiVideoListResult>(GetDefaultHttpRequestOptions(url), null).ConfigureAwait(false);
                if (videoListResult != null && videoListResult.Data != null && videoListResult.Data.Videos != null && videoListResult.Data.Videos.Count > 0)
                {
                    list.AddRange(videoListResult.Data.Videos.Where(x => !x.ShortTitle.Contains("精编版") && !x.ShortTitle.Contains("会员版")));
                }
                else
                {
                    break;
                }

                Thread.Sleep(200);
            }

            var result = new List<IqiyiEpisode>();
            list = list.OrderBy(x => x.PublishTime).ToList();
            for (int i = 0; i < list.Count; i++)
            {
                result.Add(new IqiyiEpisode() { TvId = list[i].Id, Name = list[i].ShortTitle, Order = (i + 1), Duration = list[i].Duration, PlayUrl = list[i].PlayUrl });
            }
            return result;
        }

        return new List<IqiyiEpisode>();
    }


    public async Task<List<IqiyiComment>> GetDanmuContentAsync(string tvId, CancellationToken cancellationToken)
    {
        var danmuList = new List<IqiyiComment>();
        if (string.IsNullOrEmpty(tvId))
        {
            return danmuList;
        }

        int mat = 1;
        do
        {
            try
            {
                var comments = await this.GetDanmuContentByMatAsync(tvId, mat, cancellationToken);
                // 每段有300秒弹幕，为避免弹幕太大，从中间隔抽取最大60秒200条弹幕
                danmuList.AddRange(comments.ExtractToNumber(1000));
            }
            catch (Exception ex)
            {
                break;
            }

            mat++;

            // 等待一段时间避免api请求太快
            Thread.Sleep(100);
        } while (mat < 1000);

        return danmuList;
    }

    // mat从0开始，视频分钟数
    public async Task<List<IqiyiComment>> GetDanmuContentByMatAsync(string tvId, int mat, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(tvId))
        {
            return new List<IqiyiComment>();
        }

        var s1 = tvId.Substring(tvId.Length - 4, 2);
        var s2 = tvId.Substring(tvId.Length - 2);
        // 一次拿300秒的弹幕
        var url = $"http://cmts.iqiyi.com/bullet/{s1}/{s2}/{tvId}_300_{mat}.z";
        HttpRequestOptions defaaultHttpRequestOptions = GetDefaultHttpRequestOptions(url, null, cancellationToken);
        var response = await httpClient.GetSelfResponse(defaaultHttpRequestOptions);
        if (!(response.StatusCode >= HttpStatusCode.OK && response.StatusCode <= (HttpStatusCode)299))
        {
            _logger.Info("请求http异常, httpRequestOptions={0}, status={1}", defaaultHttpRequestOptions.ToString(), response.StatusCode);
            throw new HttpRequestException("请求异常 code=" + response.StatusCode);
        }
        
        using (var zipStream = response.Content)
        {
           byte[] decompressedData = new byte[4096];
            int decompressedLength = 0;
            using (var memoryStream = new MemoryStream())
            {
                using (InflaterInputStream inflater = new InflaterInputStream(zipStream))
                {
                    do
                    {
                        decompressedLength = inflater.Read(decompressedData, 0, decompressedData.Length);
                        memoryStream.Write(decompressedData, 0, decompressedLength);
                    } while (decompressedLength > 0);
                }

                memoryStream.Position = 0;
                using (var reader = new StreamReader(memoryStream))
                {
                    var serializer = new XmlSerializer(typeof(IqiyiCommentDocument));
            
                    var result = serializer.Deserialize(reader) as IqiyiCommentDocument;
                    if (result != null && result.Data != null)
                    {
                        var comments = new List<IqiyiComment>();
                        foreach (var entry in result.Data)
                        {
                            comments.AddRange(entry.List);
                        }
                        return comments;
                    }
                }
            }

        }

        return new List<IqiyiComment>();
    }

    protected Task LimitRequestFrequently()
    {
        Thread.Sleep(1000);
        return Task.CompletedTask;
        // Task.CompletedTask;
        // await this._timeConstraint;
    }
    }
}