using System.Linq;
using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using Microsoft.Extensions.Logging;
using Jellyfin.Plugin.Danmu.Scrapers.Entity;
using System.Collections.Generic;
using System.Xml;
using Jellyfin.Plugin.Danmu.Core.Extensions;

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

    public override async Task<string?> GetMatchMediaId(BaseItem item)
    {
        var searchName = this.NormalizeSearchName(item.Name);
        var seasonId = await GetMatchBiliSeasonId(item, searchName).ConfigureAwait(false);
        if (seasonId > 0)
        {
            return $"{seasonId}";
        }

        return null;
    }


    public override async Task<ScraperMedia?> GetMedia(string id)
    {
        var media = new ScraperMedia();
        if (id.StartsWith("BV", StringComparison.CurrentCulture))
        {
            var video = await _api.GetVideoByBvidAsync(id, CancellationToken.None).ConfigureAwait(false);
            if (video == null)
            {
                log.LogInformation("获取不到b站视频信息：bvid={0}", id);
                return null;
            }


            media.Id = id;
            media.Name = video.Title;
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

        media.Id = id;
        media.Name = season.Title;
        foreach (var item in season.Episodes)
        {
            media.Episodes.Add(new ScraperEpisode() { Id = $"{item.Id}", CommentId = $"{item.CId}" });
        }

        return media;
    }

    public override async Task<ScraperEpisode?> GetMediaEpisode(string id)
    {
        var episode = new ScraperEpisode();
        if (id.StartsWith("BV", StringComparison.CurrentCulture))
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

        var epId = id.ToLong();
        if (epId <= 0)
        {
            return null;
        }

        var season = await _api.GetEpisodeAsync(epId, CancellationToken.None).ConfigureAwait(false);
        if (season == null)
        {
            log.LogInformation("获取不到b站视频信息：EpisodeId={0}", epId);
            return null;
        }

        if (season.Episodes.Length > 0)
        {
            return new ScraperEpisode() { Id = $"{season.Episodes[0].Id}", CommentId = $"{season.Episodes[0].CId}" };
        }

        return null;
    }

    public override async Task<ScraperDanmaku?> GetDanmuContent(string commentId)
    {
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
            var searchResult = await _api.SearchAsync(searchName, CancellationToken.None).ConfigureAwait(false);
            if (searchResult != null && searchResult.Result != null)
            {
                foreach (var result in searchResult.Result)
                {
                    if ((result.ResultType == "media_ft" || result.ResultType == "media_bangumi") && result.Data.Length > 0)
                    {
                        foreach (var media in result.Data)
                        {
                            var seasonId = media.SeasonId;
                            var title = media.Title;
                            var pubYear = Jellyfin.Plugin.Danmu.Core.Utils.UnixTimeStampToDateTime(media.PublishTime).Year;

                            // 检测标题是否相似（越大越相似）
                            var score = searchName.Distance(title);
                            if (score < 0.7)
                            {
                                log.LogInformation("[{0}] 标题差异太大，忽略处理. 搜索词：{1}, score:　{2}", title, searchName, score);
                                continue;
                            }

                            // 检测年份是否一致
                            var itemPubYear = item.ProductionYear ?? 0;
                            if (itemPubYear > 0 && pubYear > 0 && itemPubYear != pubYear)
                            {
                                log.LogInformation("[{0}] 发行年份不一致，忽略处理. b站：{1} jellyfin: {2}", title, pubYear, itemPubYear);
                                continue;
                            }

                            log.LogInformation("匹配成功. [{0}] seasonId: {1} score: {2}", title, seasonId, score);
                            return seasonId;
                        }
                    }
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
