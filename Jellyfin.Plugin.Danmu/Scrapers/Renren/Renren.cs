using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using Microsoft.Extensions.Logging;
using Jellyfin.Plugin.Danmu.Core;
using Jellyfin.Plugin.Danmu.Scrapers.Entity;
using Jellyfin.Plugin.Danmu.Core.Extensions;

namespace Jellyfin.Plugin.Danmu.Scrapers.Renren;

public class Renren : AbstractScraper
{
    public const string ScraperProviderName = "人人视频";
    public const string ScraperProviderId = "RenrenID";

    private readonly RenrenApi _api;

    public Renren(ILoggerFactory logManager)
        : base(logManager.CreateLogger<Renren>())
    {
        _api = new RenrenApi(logManager);
    }

    public override int DefaultOrder => 7;

    public override bool DefaultEnable => true;

    public override string Name => "人人视频";

    public override string ProviderName => ScraperProviderName;

    public override string ProviderId => ScraperProviderId;

    public override uint HashPrefix => 17;

    public override async Task<List<ScraperSearchInfo>> Search(BaseItem item)
    {
        var list = new List<ScraperSearchInfo>();
        var isMovieItemType = item is Movie;
        var searchName = this.NormalizeSearchName(item.Name);
        var items = await _api.SearchAsync(searchName, CancellationToken.None).ConfigureAwait(false);

        foreach (var result in items)
        {
            var title = System.Net.WebUtility.HtmlDecode(result.Title ?? "");
            var pubYear = result.Year;
            var category = result.Classify ?? "";

            if (isMovieItemType && category != "电影")
            {
                continue;
            }

            if (!isMovieItemType && category == "电影")
            {
                continue;
            }

            var score = searchName.Distance(title);
            if (score < 0.7)
            {
                continue;
            }

            list.Add(new ScraperSearchInfo()
            {
                Id = $"{result.Id}",
                Name = title,
                Category = category,
                Year = pubYear,
                EpisodeSize = 0,
            });
        }

        return list;
    }

    public override async Task<string?> SearchMediaId(BaseItem item)
    {
        var isMovieItemType = item is Movie;
        var searchName = this.NormalizeSearchName(item.Name);
        var items = await _api.SearchAsync(searchName, CancellationToken.None).ConfigureAwait(false);

        foreach (var result in items)
        {
            var title = System.Net.WebUtility.HtmlDecode(result.Title ?? "");
            var pubYear = result.Year;
            var category = result.Classify ?? "";

            if (isMovieItemType && category != "电影")
            {
                continue;
            }

            if (!isMovieItemType && category == "电影")
            {
                continue;
            }

            var score = searchName.Distance(title);
            if (score < 0.7)
            {
                log.LogDebug("[{0}] 标题差异太大，忽略处理. 搜索词：{1}, score: {2}", title, searchName, score);
                continue;
            }

            var itemPubYear = item.ProductionYear ?? 0;
            if (itemPubYear > 0 && pubYear.HasValue && pubYear.Value > 0 && itemPubYear != pubYear.Value)
            {
                log.LogDebug("[{0}] 发行年份不一致，忽略处理. year: {1} jellyfin: {2}", title, pubYear, itemPubYear);
                continue;
            }

            return result.Id;
        }

        return null;
    }

    public override async Task<ScraperMedia?> GetMedia(BaseItem item, string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        var isMovieItemType = item is Movie;
        var detail = await _api.GetDetailAsync(id, "", CancellationToken.None).ConfigureAwait(false);
        if (detail == null)
        {
            log.LogInformation("[{0}]获取不到视频信息：id={1}", this.Name, id);
            return null;
        }

        var media = new ScraperMedia();
        media.Id = id;

        if (detail.EpisodeList != null && detail.EpisodeList.Count > 0)
        {
            if (isMovieItemType)
            {
                media.CommentId = detail.EpisodeList[0].Sid;
            }

            foreach (var ep in detail.EpisodeList)
            {
                media.Episodes.Add(new ScraperEpisode()
                {
                    Id = $"{ep.Sid}",
                    CommentId = ep.Sid,
                    Title = ep.Title ?? $"第{ep.EpisodeNo}集",
                });
            }
        }

        return media;
    }

    public override async Task<ScraperEpisode?> GetMediaEpisode(BaseItem item, string id)
    {
        var isMovieItemType = item is Movie;
        if (isMovieItemType)
        {
            var detail = await _api.GetDetailAsync(id, "", CancellationToken.None).ConfigureAwait(false);
            if (detail == null || detail.EpisodeList == null || detail.EpisodeList.Count <= 0)
            {
                return null;
            }

            var firstEp = detail.EpisodeList[0];
            return new ScraperEpisode()
            {
                Id = id,
                CommentId = firstEp.Sid,
                Title = firstEp.Title ?? $"第{firstEp.EpisodeNo}集",
            };
        }

        if (item is Episode episode)
        {
            var season = episode.Season;
            if (season != null)
            {
                DanmuProviderId.TryGet(season, ScraperProviderId, out var seriesId);
                if (!string.IsNullOrEmpty(seriesId))
                {
                    return new ScraperEpisode() { Id = id, CommentId = id };
                }
            }
        }

        return new ScraperEpisode() { Id = id, CommentId = id };
    }

    public override async Task<ScraperDanmaku?> GetDanmuContent(BaseItem item, string commentId)
    {
        if (string.IsNullOrEmpty(commentId))
        {
            return null;
        }

        var danmuItems = await _api.GetDanmuAsync(commentId, CancellationToken.None).ConfigureAwait(false);

        var danmaku = new ScraperDanmaku();
        danmaku.ChatId = 0;
        danmaku.ChatServer = "static-dm.qwdjapp.com";

        foreach (var danmu in danmuItems)
        {
            var text = danmu.D;
            if (string.IsNullOrEmpty(text) && danmu.Content != null)
            {
                text = danmu.Content;
            }

            if (string.IsNullOrEmpty(text))
            {
                continue;
            }

            var danmakuText = new ScraperDanmakuText();
            var arr = danmu.P.Split(",");

            if (arr.Length >= 4)
            {
                danmakuText.Progress = (int)(Convert.ToDouble(arr[0]) * 1000);
                danmakuText.Mode = Convert.ToInt32(arr[1]);
                danmakuText.Color = Convert.ToUInt32(arr[3]);
                danmakuText.MidHash = "[renren]" + (arr.Length > 6 ? arr[6] : "");
                danmakuText.Content = text;
                danmaku.Items.Add(danmakuText);
            }
        }

        return danmaku;
    }

    public override async Task<List<ScraperSearchInfo>> SearchForApi(string keyword)
    {
        var list = new List<ScraperSearchInfo>();
        var items = await _api.SearchAsync(keyword, CancellationToken.None).ConfigureAwait(false);

        foreach (var result in items)
        {
            list.Add(new ScraperSearchInfo()
            {
                Id = $"{result.Id}",
                Name = System.Net.WebUtility.HtmlDecode(result.Title ?? ""),
                Category = result.Classify ?? "",
                Year = result.Year,
                EpisodeSize = 0,
            });
        }

        return list;
    }

    public override async Task<List<ScraperEpisode>> GetEpisodesForApi(string id)
    {
        var list = new List<ScraperEpisode>();
        if (string.IsNullOrEmpty(id))
        {
            return list;
        }

        var detail = await _api.GetDetailAsync(id, "", CancellationToken.None).ConfigureAwait(false);
        if (detail == null || detail.EpisodeList == null)
        {
            return list;
        }

        foreach (var ep in detail.EpisodeList)
        {
            list.Add(new ScraperEpisode()
            {
                Id = $"{ep.Sid}",
                CommentId = $"{id},{ep.Sid}",
                Title = ep.Title ?? $"第{ep.EpisodeNo}集",
            });
        }

        return list;
    }

    public override async Task<ScraperDanmaku?> DownloadDanmuForApi(string commentId)
    {
        return await this.GetDanmuContent(null, commentId).ConfigureAwait(false);
    }
}
