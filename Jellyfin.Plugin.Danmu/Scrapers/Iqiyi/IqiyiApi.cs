using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using ComposableAsync;
using Jellyfin.Plugin.Danmu.Core.Extensions;
using Jellyfin.Plugin.Danmu.Scrapers.Iqiyi.Entity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using RateLimiter;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using System.Xml.Serialization;
using System.Net.Http;

namespace Jellyfin.Plugin.Danmu.Scrapers.Iqiyi;

public class IqiyiApi : AbstractApi
{
    private const string HTTP_USER_AGENT = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/115.0.0.0 Safari/537.36 Edg/115.0.1901.183";
    private const string MOBILE_USER_AGENT = "Mozilla/5.0 (Linux; Android 6.0; Nexus 5 Build/MRA58N) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Mobile Safari/537.36";
    private static readonly Regex regVideoInfo = new Regex(@"""videoInfo"":(\{.+?\}),""", RegexOptions.Compiled);
    private static readonly Regex regAlbumInfo = new Regex(@"""albumInfo"":(\{.+?\}),""", RegexOptions.Compiled);


    private TimeLimiter _timeConstraint = TimeLimiter.GetFromMaxCountByInterval(1, TimeSpan.FromMilliseconds(1000));
    private TimeLimiter _delayExecuteConstraint = TimeLimiter.GetFromMaxCountByInterval(1, TimeSpan.FromMilliseconds(100));

    protected string _cna = string.Empty;
    protected string _token = string.Empty;
    protected string _tokenEnc = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="IqiyiApi"/> class.
    /// </summary>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    public IqiyiApi(ILoggerFactory loggerFactory)
        : base(loggerFactory.CreateLogger<IqiyiApi>())
    {
        httpClient.DefaultRequestHeaders.Add("user-agent", HTTP_USER_AGENT);
    }


    public async Task<List<IqiyiSearchAlbumInfo>> SearchAsync(string keyword, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(keyword))
        {
            return new List<IqiyiSearchAlbumInfo>();
        }

        var cacheKey = $"search_{keyword}";
        var expiredOption = new MemoryCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) };
        if (_memoryCache.TryGetValue<List<IqiyiSearchAlbumInfo>>(cacheKey, out var cacheValue))
        {
            return cacheValue;
        }

        await this.LimitRequestFrequently();

        keyword = HttpUtility.UrlEncode(keyword);
        var url = $"https://search.video.iqiyi.com/o?if=html5&key={keyword}&pageNum=1&pageSize=20";
        var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var result = new List<IqiyiSearchAlbumInfo>();
        var searchResult = await response.Content.ReadFromJsonAsync<IqiyiSearchResult>(_jsonOptions, cancellationToken).ConfigureAwait(false);
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
        if (_memoryCache.TryGetValue<IqiyiHtmlVideoInfo?>(cacheKey, out var video))
        {
            return video;
        }

        // 获取电视剧信息(aid)：https://pcw-api.iqiyi.com/album/album/baseinfo/5328486914190101
        // 获取电视剧剧集信息(综艺不适用)(aid)：https://pcw-api.iqiyi.com/albums/album/avlistinfo?aid=5328486914190101&page=1&size=10
        // 获取电视剧剧集信息(综艺不适用)(aid)：https://pub.m.iqiyi.com/h5/main/videoList/album/?albumId=5328486914190101&size=39&page=1&needPrevue=true&needVipPrevue=false
        var videoInfo = await GetVideoBaseAsync(id, cancellationToken).ConfigureAwait(false);
        if (videoInfo == null)
        {
            _logger.LogInformation("获取不到视频信息：id={1}", id);
            _memoryCache.Set<IqiyiHtmlVideoInfo?>(cacheKey, null, expiredOption);
            return null;
        }

        if (videoInfo.channelName == "综艺")
        { // 综艺需要特殊处理
            videoInfo.Epsodelist = await this.GetZongyiEpisodesAsync($"{videoInfo.AlbumId}", cancellationToken).ConfigureAwait(false);
        }
        else if (videoInfo.channelName == "电影")
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

    public async Task<IqiyiHtmlVideoInfo?> GetVideoBaseAsync(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        var cacheKey = $"video_base_{id}";
        var expiredOption = new MemoryCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) };
        if (_memoryCache.TryGetValue<IqiyiHtmlVideoInfo?>(cacheKey, out var video))
        {
            return video;
        }

        await this.LimitRequestFrequently();

        var url = $"https://m.iqiyi.com/v_{id}.html";
        using (var request = new HttpRequestMessage(HttpMethod.Get, url))
        {
            request.Headers.Add("user-agent", MOBILE_USER_AGENT);
            var response = await this.httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var albumJson = regAlbumInfo.FirstMatchGroup(body);
            var albumInfo = albumJson.FromJson<IqiyiHtmlAlbumInfo>();
            var videoJson = regVideoInfo.FirstMatchGroup(body);
            var videoInfo = videoJson.FromJson<IqiyiHtmlVideoInfo>();
            if (videoInfo != null)
            {
                if (albumInfo != null)
                {
                    videoInfo.VideoCount = albumInfo.VideoCount;
                }
                this._memoryCache.Set(cacheKey, videoInfo, expiredOption);
                return videoInfo;
            }
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
        var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var albumResult = await response.Content.ReadFromJsonAsync<IqiyiVideoResult>(_jsonOptions, cancellationToken).ConfigureAwait(false);
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
        var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var albumResult = await response.Content.ReadFromJsonAsync<IqiyiAlbumResult>(_jsonOptions, cancellationToken).ConfigureAwait(false);
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
                response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                var videoListResult = await response.Content.ReadFromJsonAsync<IqiyiVideoListResult>(_jsonOptions, cancellationToken).ConfigureAwait(false);
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
            await _delayExecuteConstraint;
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
        var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        using (var zipStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
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

    protected async Task LimitRequestFrequently()
    {
        await this._timeConstraint;
    }

}

