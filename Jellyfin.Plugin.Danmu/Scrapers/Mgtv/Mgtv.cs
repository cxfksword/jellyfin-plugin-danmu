using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using Microsoft.Extensions.Logging;
using Jellyfin.Plugin.Danmu.Scrapers.Entity;
using System.Collections.Generic;
using Jellyfin.Plugin.Danmu.Core.Extensions;

namespace Jellyfin.Plugin.Danmu.Scrapers.Mgtv;

public class Mgtv : AbstractScraper
{
    public const string ScraperProviderName = "芒果TV";
    public const string ScraperProviderId = "MgtvID";

    private readonly MgtvApi _api;

    public Mgtv(ILoggerFactory logManager)
        : base(logManager.CreateLogger<Mgtv>())
    {
        _api = new MgtvApi(logManager);
    }

    public override int DefaultOrder => 6;

    public override bool DefaultEnable => true;

    public override string Name => "芒果TV";

    public override string ProviderName => ScraperProviderName;

    public override string ProviderId => ScraperProviderId;

    public override uint HashPrefix => 15;


    private static readonly Regex regTvEpisodeTitle = new Regex(@"^第.+?集$", RegexOptions.Compiled);

    public override async Task<List<ScraperSearchInfo>> Search(BaseItem item)
    {
        var list = new List<ScraperSearchInfo>();
        var isMovieItemType = item is MediaBrowser.Controller.Entities.Movies.Movie;
        var searchName = this.NormalizeSearchName(item.Name);
        var videos = await this._api.SearchAsync(searchName, CancellationToken.None).ConfigureAwait(false);
        foreach (var video in videos)
        {
            var videoId = video.Id;
            var title = video.Title;
            var pubYear = video.Year;

            if (isMovieItemType && video.TypeName != "电影")
            {
                continue;
            }

            if (!isMovieItemType && video.TypeName == "电影")
            {
                continue;
            }

            // 检测标题是否相似（越大越相似）
            var score = searchName.Distance(title);
            if (score < 0.7)
            {
                continue;
            }

            list.Add(new ScraperSearchInfo()
            {
                Id = $"{videoId}",
                Name = title,
                Category = video.TypeName,
                Year = pubYear,
                EpisodeSize = video.VideoCount,
            });
        }


        return list;
    }

    public override async Task<string?> SearchMediaId(BaseItem item)
    {
        var isMovieItemType = item is MediaBrowser.Controller.Entities.Movies.Movie;
        var searchName = this.NormalizeSearchName(item.Name);
        var videos = await this._api.SearchAsync(searchName, CancellationToken.None).ConfigureAwait(false);
        foreach (var video in videos)
        {
            var videoId = video.Id;
            var title = video.Title;
            var pubYear = video.Year;

            if (isMovieItemType && video.TypeName != "电影")
            {
                continue;
            }

            if (!isMovieItemType && video.TypeName == "电影")
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
                log.LogDebug("[{0}] 发行年份不一致，忽略处理. year: {1} jellyfin: {2}", title, pubYear, itemPubYear);
                continue;
            }

            return video.Id;
        }

        return null;
    }


    public override async Task<ScraperMedia?> GetMedia(BaseItem item, string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        var isMovieItemType = item is MediaBrowser.Controller.Entities.Movies.Movie;
        var video = await _api.GetVideoAsync(id, CancellationToken.None).ConfigureAwait(false);
        if (video == null)
        {
            log.LogInformation("[{0}]获取不到视频信息：id={1}", this.Name, id);
            return null;
        }


        var media = new ScraperMedia();
        media.Id = id;
        if (isMovieItemType && video.EpisodeList != null && video.EpisodeList.Count > 0)
        {
            media.CommentId = $"{id},{video.EpisodeList[0].VideoId}";
        }
        if (video.EpisodeList != null && video.EpisodeList.Count > 0)
        {
            foreach (var ep in video.EpisodeList)
            {
                media.Episodes.Add(new ScraperEpisode() { Id = $"{ep.VideoId}", CommentId = $"{id},{ep.VideoId}" });
            }
        }

        return media;
    }

    public override async Task<ScraperEpisode?> GetMediaEpisode(BaseItem item, string id)
    {

        var isMovieItemType = item is MediaBrowser.Controller.Entities.Movies.Movie;
        if (isMovieItemType)
        {
            var video = await _api.GetVideoAsync(id, CancellationToken.None).ConfigureAwait(false);
            if (video == null || video.EpisodeList == null || video.EpisodeList.Count <= 0)
            {
                return null;
            }

            return new ScraperEpisode() { Id = id, CommentId = $"{id},{video.EpisodeList[0].VideoId}" };
        }


        // 从季信息元数据中，获取cid值
        // 不能通过GetParent获取Season，因为没有SXX季文件夹时，GetParent是Series
        var season = ((MediaBrowser.Controller.Entities.TV.Episode)item).Season;
        season.ProviderIds.TryGetValue(ScraperProviderId, out var cid);
        return new ScraperEpisode() { Id = id, CommentId = $"{cid},{id}" };
    }

    public override async Task<ScraperDanmaku?> GetDanmuContent(BaseItem item, string commentId)
    {
        if (string.IsNullOrEmpty(commentId))
        {
            return null;
        }

        var arr = commentId.Split(",");
        if (arr.Length < 2)
        {
            return null;
        }

        var cid = arr[0];
        var vid = arr[1];
        if (string.IsNullOrEmpty(cid) || string.IsNullOrEmpty(vid))
        {
            return null;
        }
        var comments = await _api.GetDanmuContentAsync(cid, vid, CancellationToken.None).ConfigureAwait(false);
        var danmaku = new ScraperDanmaku();
        danmaku.ChatId = vid.ToLong();
        danmaku.ChatServer = "galaxy.bz.mgtv.com";
        foreach (var comment in comments)
        {

            var danmakuText = new ScraperDanmakuText();
            danmakuText.Progress = comment.Time;
            danmakuText.Mode = 1;
            danmakuText.MidHash = $"[mgtv]{comment.Uuid}";
            danmakuText.Id = comment.Id;
            danmakuText.Content = comment.Content;
            if (comment.Color != null && comment.Color.ColorLeft != null)
            {
                danmakuText.Color = comment.Color.ColorLeft.HexNumber;
            }

            danmaku.Items.Add(danmakuText);
        }

        return danmaku;
    }


    public override async Task<List<ScraperSearchInfo>> SearchForApi(string keyword)
    {
        var list = new List<ScraperSearchInfo>();
        var videos = await this._api.SearchAsync(keyword, CancellationToken.None).ConfigureAwait(false);
        foreach (var video in videos)
        {
            var videoId = video.Id;
            var title = video.Title;
            var pubYear = video.Year;
            list.Add(new ScraperSearchInfo()
            {
                Id = $"{videoId}",
                Name = title,
                Category = video.TypeName,
                Year = pubYear,
                EpisodeSize = video.VideoCount,
            });
        }
        return list;
    }

    public override async Task<List<ScraperEpisode>> GetEpisodesForApi(string id)
    {
        var list = new List<ScraperEpisode>();
        var video = await this._api.GetVideoAsync(id, CancellationToken.None).ConfigureAwait(false);
        if (video == null)
        {
            return list;
        }

        if (video.EpisodeList != null && video.EpisodeList.Count > 0)
        {
            foreach (var ep in video.EpisodeList)
            {
                var title = regTvEpisodeTitle.IsMatch(ep.Title2) ? ep.Title2 : ep.Title;
                list.Add(new ScraperEpisode() { Id = $"{ep.VideoId}", CommentId = $"{id},{ep.VideoId}", Title = title });
            }
        }

        return list;
    }

    public override async Task<ScraperDanmaku?> DownloadDanmuForApi(string commentId)
    {
        return await this.GetDanmuContent(null, commentId).ConfigureAwait(false);
    }
}
