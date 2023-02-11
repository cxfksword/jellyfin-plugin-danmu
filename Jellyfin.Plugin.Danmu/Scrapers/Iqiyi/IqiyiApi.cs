using System.IO;
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
using Jellyfin.Plugin.Danmu.Scrapers.Iqiyi.Entity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using RateLimiter;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using System.Xml.Serialization;

namespace Jellyfin.Plugin.Danmu.Scrapers.Iqiyi;

public class IqiyiApi : AbstractApi
{
    const string HTTP_USER_AGENT = "Mozilla/5.0 (iPhone; CPU iPhone OS 13_2_3 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/13.0.3 Mobile/15E148 Safari/604.1 Edg/93.0.4577.63";
    private static readonly object _lock = new object();
    private static readonly Regex yearReg = new Regex(@"[12][890][0-9][0-9]", RegexOptions.Compiled);
    private static readonly Regex moviesReg = new Regex(@"<a.*?h5-show-card.*?>([\w\W]+?)</a>", RegexOptions.Compiled);
    private static readonly Regex trackInfoReg = new Regex(@"data-trackinfo=""(\{[\w\W]+?\})""", RegexOptions.Compiled);
    private static readonly Regex featureReg = new Regex(@"<div.*?show-feature.*?>([\w\W]+?)</div>", RegexOptions.Compiled);
    private static readonly Regex unusedReg = new Regex(@"\[.+?\]|\(.+?\)|【.+?】", RegexOptions.Compiled);
    private static readonly Regex regTvId = new Regex(@"""tvid"":(\d+?),", RegexOptions.Compiled);


    private DateTime lastRequestTime = DateTime.Now.AddDays(-1);

    private TimeLimiter _timeConstraint = TimeLimiter.GetFromMaxCountByInterval(1, TimeSpan.FromMilliseconds(1000));

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


    public async Task<List<IqiyiSuggest>> GetSuggestAsync(string keyword, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(keyword))
        {
            return new List<IqiyiSuggest>();
        }

        var cacheKey = $"search_{keyword}";
        var expiredOption = new MemoryCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) };
        if (_memoryCache.TryGetValue<List<IqiyiSuggest>>(cacheKey, out var cacheValue))
        {
            return cacheValue;
        }

        keyword = HttpUtility.UrlEncode(keyword);
        var url = $"https://suggest.video.iqiyi.com/?key={keyword}&platform=11&rltnum=10&ppuid=";
        var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var result = new List<IqiyiSuggest>();
        var searchResult = await response.Content.ReadFromJsonAsync<IqiyiSuggestResult>(_jsonOptions, cancellationToken).ConfigureAwait(false);
        if (searchResult != null && searchResult.Data != null)
        {
            result = searchResult.Data.Where(x => !string.IsNullOrEmpty(x.Link) && x.Link.Contains("iqiyi.com") && x.VideoId > 0).ToList();
        }

        _memoryCache.Set<List<IqiyiSuggest>>(cacheKey, result, expiredOption);
        return result;
    }

    public async Task<IqiyiVideo?> GetVideoAsync(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        var cacheKey = $"video_{id}";
        var expiredOption = new MemoryCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) };
        if (_memoryCache.TryGetValue<IqiyiVideo?>(cacheKey, out var video))
        {
            return video;
        }

        // 获取影片信息(vid)：https://pcw-api.iqiyi.com/video/video/baseinfo/429872
        // 获取电视剧信息(aid)：https://pcw-api.iqiyi.com/album/album/baseinfo/5328486914190101
        // 获取影片剧集信息(aid)：https://pcw-api.iqiyi.com/albums/album/avlistinfo?aid=5328486914190101&page=1&size=100
        var url = $"https://pcw-api.iqiyi.com/video/video/baseinfo/{id}";
        var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<IqiyiVideoResult>(_jsonOptions, cancellationToken).ConfigureAwait(false);
        if (result != null && result.Data != null)
        {
            // 电视剧需要再获取剧集信息
            if (result.Data.ChannelId != 1)
            {
                result.Data.Epsodelist = await this.GetEpisodesAsync($"{result.Data.AlbumId}", result.Data.VideoCount, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                result.Data.Epsodelist = new List<IqiyiEpisode>() {
                    new IqiyiEpisode() {TvId = result.Data.TvId, Order = 1, Name = result.Data.Name, Duration = result.Data.Duration, PlayUrl = result.Data.PlayUrl}
                };
            }

            _memoryCache.Set<IqiyiVideo?>(cacheKey, result.Data, expiredOption);
            return result.Data;
        }

        _memoryCache.Set<IqiyiVideo?>(cacheKey, null, expiredOption);
        return null;
    }

    public async Task<List<IqiyiEpisode>> GetEpisodesAsync(string albumId, int size, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(albumId))
        {
            return new List<IqiyiEpisode>();
        }


        var url = $"https://pcw-api.iqiyi.com/albums/album/avlistinfo?aid={albumId}&page=1&size={size}";
        var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var albumResult = await response.Content.ReadFromJsonAsync<IqiyiAlbumResult>(_jsonOptions, cancellationToken).ConfigureAwait(false);
        if (albumResult != null && albumResult.Data != null && albumResult.Data.Epsodelist != null)
        {
            return albumResult.Data.Epsodelist;
        }

        return new List<IqiyiEpisode>();
    }

    public async Task<string> GetTvId(string id, bool isAlbum, CancellationToken cancellationToken)
    {
        var cacheKey = $"tvid_{id}";
        var expiredOption = new MemoryCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) };
        if (_memoryCache.TryGetValue<string>(cacheKey, out var tvid))
        {
            return tvid;
        }

        var url = isAlbum ? $"https://m.iqiyi.com/a_{id}.html" : $"https://m.iqiyi.com/v_{id}.html";
        var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        tvid = regTvId.FirstMatchGroup(body);
        _memoryCache.Set<string>(cacheKey, tvid, expiredOption);

        return tvid;
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
                danmuList.AddRange(comments);
            }
            catch (Exception ex)
            {
                break;
            }

            mat++;
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

