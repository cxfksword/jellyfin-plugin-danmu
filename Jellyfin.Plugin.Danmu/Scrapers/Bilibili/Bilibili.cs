using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Jellyfin.Plugin.Danmu.Core.Extensions;
using Jellyfin.Plugin.Danmu.Scrapers.Entity;
using MediaBrowser.Controller.Entities;
using Microsoft.Extensions.Logging;


namespace Jellyfin.Plugin.Danmu.Scrapers.Bilibili;

public class Bilibili : AbstractScraper
{
    public const string ScraperProviderName = "Bilibili";
    public const string ScraperProviderId = "BilibiliID";

    private readonly BilibiliApi _api;

    public Bilibili(ILoggerFactory logManager)
        : base(logManager.CreateLogger<Bilibili>())
    {
        _api = new BilibiliApi(logManager);
    }

    public override int DefaultOrder => 1;

    public override bool DefaultEnable => true;

    public override string Name => "bilibili";

    public override string ProviderName => ScraperProviderName;

    public override string ProviderId => ScraperProviderId;

    public override async Task<List<ScraperSearchInfo>> Search(BaseItem item)
    {
        var list = new List<ScraperSearchInfo>();
        var isMovieItemType = item is MediaBrowser.Controller.Entities.Movies.Movie;
        var searchName = this.NormalizeSearchName(item.Name);
        try
        {
            var searchResult = await _api.SearchAsync(searchName, CancellationToken.None).ConfigureAwait(false);
            if (searchResult != null && searchResult.Result != null)
            {
                foreach (var media in searchResult.Result)
                {
                    if (media.Type != "media_ft" && media.Type != "media_bangumi")
                    {
                        continue;
                    }

                    var seasonId = media.SeasonId;
                    var title = media.Title;
                    var pubYear = Jellyfin.Plugin.Danmu.Core.Utils.UnixTimeStampToDateTime(media.PublishTime).Year;

                    list.Add(new ScraperSearchInfo()
                    {
                        Id = $"{seasonId}",
                        Name = title,
                        Category = media.SeasonTypeName,
                        Year = pubYear,
                        EpisodeSize = media.EpisodeSize,
                    });
                }
            }
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Exception handled GetMatchSeasonId. {0}", searchName);
        }

        return list;
    }

    public override async Task<string?> SearchMediaId(BaseItem item)
    {
        var searchName = this.NormalizeSearchName(item.Name);
        var seasonId = await GetMatchBiliSeasonId(item, searchName).ConfigureAwait(false);
        if (seasonId > 0)
        {
            return $"{seasonId}";
        }

        return null;
    }


    public override async Task<ScraperMedia?> GetMedia(BaseItem item, string id)
    {
        var media = new ScraperMedia();
        var isMovieItemType = item is MediaBrowser.Controller.Entities.Movies.Movie;

        if (id.StartsWith("BV", StringComparison.CurrentCultureIgnoreCase))
        {
            var video = await _api.GetVideoByBvidAsync(id, CancellationToken.None).ConfigureAwait(false);
            if (video == null)
            {
                log.LogInformation("获取不到b站视频信息：bvid={0}", id);
                return null;
            }

            if (video.UgcSeason != null && video.UgcSeason.Sections != null && video.UgcSeason.Sections.Count > 0)
            {
                // 合集
                foreach (var (page, idx) in video.UgcSeason.Sections[0].Episodes.WithIndex())
                {
                    media.Episodes.Add(new ScraperEpisode() { Id = "", CommentId = $"{page.CId}" });
                }
            }
            else
            {
                // 分P
                foreach (var (page, idx) in video.Pages.WithIndex())
                {
                    media.Episodes.Add(new ScraperEpisode() { Id = "", CommentId = $"{page.Cid}" });
                }
            }

            if (isMovieItemType)
            {
                media.Id = id;
                media.CommentId = media.Episodes.Count > 0 ? $"{media.Episodes[0].CommentId}" : "";
            }
            else
            {
                media.Id = id;
            }

            return media;
        }

        if (id.StartsWith("av", StringComparison.CurrentCultureIgnoreCase))
        {
            var biliplusVideo = await _api.GetVideoByAvidAsync(id, CancellationToken.None).ConfigureAwait(false);
            if (biliplusVideo == null)
            {
                log.LogInformation("获取不到b站视频信息：avid={0}", id);
                return null;
            }

            var aid = id.Substring(2);

            // 分P
            foreach (var (page, idx) in biliplusVideo.List.WithIndex())
            {
                media.Episodes.Add(new ScraperEpisode() { Id = "", CommentId = $"{aid},{page.Cid}" });
            }


            if (isMovieItemType)
            {
                media.Id = id;
                media.CommentId = media.Episodes.Count > 0 ? $"{aid},{media.Episodes[0].CommentId}" : "";
            }
            else
            {
                media.Id = id;
            }

            return media;
        }

        var seasonId = id.ToLong();
        if (seasonId <= 0)
        {
            return null;
        }

        var season = await _api.GetSeasonAsync(seasonId, CancellationToken.None).ConfigureAwait(false);
        if (season == null)
        {
            log.LogInformation("获取不到b站视频信息：seasonId={0}", seasonId);
            return null;
        }

        foreach (var ep in season.Episodes)
        {
            media.Episodes.Add(new ScraperEpisode() { Id = $"{ep.Id}", CommentId = $"{ep.CId}" });
        }

        if (isMovieItemType)
        {
            media.Id = season.Episodes.Count > 0 ? $"{season.Episodes[0].Id}" : "";
            media.CommentId = season.Episodes.Count > 0 ? $"{season.Episodes[0].CId}" : "";
        }
        else
        {
            media.Id = id;
        }

        return media;
    }

    public override async Task<ScraperEpisode?> GetMediaEpisode(BaseItem item, string id)
    {
        var episode = new ScraperEpisode();
        if (id.StartsWith("BV", StringComparison.CurrentCultureIgnoreCase))
        {
            var video = await _api.GetVideoByBvidAsync(id, CancellationToken.None).ConfigureAwait(false);
            if (video == null)
            {
                log.LogInformation("获取不到b站视频信息：bvid={0}", id);
                return null;
            }


            if (video.Pages.Length > 0)
            {
                return new ScraperEpisode() { Id = "", CommentId = $"{video.Pages[0].Cid}" };
            }

            return null;
        }

        if (id.StartsWith("av", StringComparison.CurrentCultureIgnoreCase))
        {
            var biliplusVideo = await _api.GetVideoByAvidAsync(id, CancellationToken.None).ConfigureAwait(false);
            if (biliplusVideo == null)
            {
                log.LogInformation("获取不到b站视频信息：avid={0}", id);
                return null;
            }


            if (biliplusVideo.List.Length > 0)
            {
                var aid = id.Substring(2);
                return new ScraperEpisode() { Id = "", CommentId = $"{aid},{biliplusVideo.List[0].Cid}" };
            }

            return null;
        }

        var epId = id.ToLong();
        if (epId <= 0)
        {
            return null;
        }

        var epInfo = await _api.GetEpisodeAsync(epId, CancellationToken.None).ConfigureAwait(false);
        if (epInfo == null)
        {
            log.LogInformation("获取不到b站视频信息：EpisodeId={0}", epId);
            return null;
        }

        return new ScraperEpisode() { Id = $"{epInfo.Id}", CommentId = $"{epInfo.CId}" };
    }

    public override async Task<ScraperDanmaku?> GetDanmuContent(BaseItem item, string commentId)
    {
        if (commentId.Contains(","))
        {
            var arr = commentId.Split(",");
            if (arr.Length == 2)
            {
                var aid = arr[0].ToLong();
                var cmid = arr[1].ToLong();
                if (aid > 0 && cmid > 0)
                {
                    return await _api.GetDanmuContentByProtoAsync(aid, cmid, CancellationToken.None).ConfigureAwait(false);
                }
            }
            return null;
        }


        var cid = commentId.ToLong();
        if (cid > 0)
        {
            var bytes = await _api.GetDanmuContentByCidAsync(cid, CancellationToken.None).ConfigureAwait(false);
            var danmaku = ParseXml(System.Text.Encoding.UTF8.GetString(bytes));
            danmaku.ChatId = cid;
            return danmaku;
        }

        return null;
    }


    private ScraperDanmaku ParseXml(string xml)
    {
        var doc = new XmlDocument();
        doc.LoadXml(xml);

        var calFontSizeDict = new Dictionary<int, int>();
        var biliDanmakus = new ScraperDanmaku();
        var nodes = doc.GetElementsByTagName("d");
        foreach (XmlNode node in nodes)
        {
            // bilibili弹幕格式：
            // <d p="944.95400,5,25,16707842,1657598634,0,ece5c9d1,1094775706690331648,11">今天的风儿甚是喧嚣</d>
            // time, mode, size, color, create, pool, sender, id, weight(屏蔽等级)
            var p = node.Attributes["p"];
            if (p == null)
            {
                continue;
            }

            var danmaku = new ScraperDanmakuText();
            var arr = p.Value.Split(",");
            danmaku.Progress = (int)(Convert.ToDouble(arr[0]) * 1000);
            danmaku.Mode = Convert.ToInt32(arr[1]);
            danmaku.Fontsize = Convert.ToInt32(arr[2]);
            danmaku.Color = Convert.ToUInt32(arr[3]);
            danmaku.Ctime = Convert.ToInt64(arr[4]);
            danmaku.Pool = Convert.ToInt32(arr[5]);
            danmaku.MidHash = arr[6];
            danmaku.Id = Convert.ToInt64(arr[7]);
            danmaku.Weight = Convert.ToInt32(arr[8]);
            danmaku.Content = node.InnerText;

            biliDanmakus.Items.Add(danmaku);

            if (calFontSizeDict.ContainsKey(danmaku.Fontsize))
            {
                calFontSizeDict[danmaku.Fontsize]++;
            }
            else
            {
                calFontSizeDict[danmaku.Fontsize] = 1;
            }
        }

        // 按弹幕出现顺序排序
        biliDanmakus.Items.Sort((x, y) => { return x.Progress.CompareTo(y.Progress); });

        return biliDanmakus;
    }

    private string NormalizeSearchName(string name)
    {
        // 去掉可能存在的季名称
        return Regex.Replace(name, @"\s*第.季", "");
    }



    // 根据名称搜索对应的seasonId
    private async Task<long> GetMatchBiliSeasonId(BaseItem item, string searchName)
    {
        try
        {
            var isMovieItemType = item is MediaBrowser.Controller.Entities.Movies.Movie;
            var isSeasonItemType = item is MediaBrowser.Controller.Entities.TV.Season;
            var searchResult = await _api.SearchAsync(searchName, CancellationToken.None).ConfigureAwait(false);
            if (searchResult != null && searchResult.Result != null)
            {
                foreach (var media in searchResult.Result)
                {
                    if (media.Type != "media_ft" && media.Type != "media_bangumi")
                    {
                        continue;
                    }

                    var seasonId = media.SeasonId;
                    var title = media.Title;
                    var pubYear = Jellyfin.Plugin.Danmu.Core.Utils.UnixTimeStampToDateTime(media.PublishTime).Year;

                    if (isMovieItemType && media.SeasonTypeName != "电影")
                    {
                        continue;
                    }

                    if (!isMovieItemType && media.SeasonTypeName == "电影")
                    {
                        continue;
                    }

                    // 检测标题是否相似（越大越相似）
                    var score = searchName.Distance(title);
                    if (score < 0.7)
                    {
                        log.LogDebug("[{0}] 标题差异太大，忽略处理. 搜索词：{1}, score:　{2}", title, searchName, score);
                        continue;
                    }

                    // 检测年份是否一致
                    var itemPubYear = item.ProductionYear ?? 0;
                    if (itemPubYear > 0 && pubYear > 0 && itemPubYear != pubYear)
                    {
                        log.LogDebug("[{0}] 发行年份不一致，忽略处理. b站：{1} jellyfin: {2}", title, pubYear, itemPubYear);
                        continue;
                    }

                    // 季匹配处理，没有year但有season_number时，判断后缀是否有对应的第几季，如孤独的美食家
                    if (isSeasonItemType && itemPubYear == 0 && item.IndexNumber != null && item.IndexNumber.Value > 1 && media.SeasonNumber != item.IndexNumber)
                    {
                        log.LogDebug("[{0}] 季号不一致，忽略处理. b站：{1} jellyfin: {2}", title, media.SeasonNumber, item.IndexNumber);
                        continue;
                    }

                    log.LogInformation("匹配成功. [{0}] seasonId: {1} score: {2}", title, seasonId, score);
                    return seasonId;
                }

            }
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == System.Net.HttpStatusCode.PreconditionFailed)
            {
                throw new FrequentlyRequestException(ex);
            }
            log.LogError(ex, "Exception handled GetMatchSeasonId. {0}", searchName);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Exception handled GetMatchSeasonId. {0}", searchName);
        }

        return 0;
    }
}
