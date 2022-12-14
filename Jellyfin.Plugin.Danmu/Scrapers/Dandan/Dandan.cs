using System.Linq;
using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Danmu.Core;
using MediaBrowser.Controller.Entities;
using Microsoft.Extensions.Logging;
using Jellyfin.Plugin.Danmu.Scrapers.Entity;
using System.Collections.Generic;
using System.Xml;
using Jellyfin.Plugin.Danmu.Core.Extensions;

namespace Jellyfin.Plugin.Danmu.Scrapers.Dandan;

public class Dandan : AbstractScraper
{
    public const string ScraperProviderName = "弹弹play";
    public const string ScraperProviderId = "DandanID";

    private readonly DandanApi _api;

    public Dandan(ILoggerFactory logManager)
        : base(logManager.CreateLogger<Dandan>())
    {
        _api = new DandanApi(logManager);
    }

    public override int DefaultOrder => 2;

    public override bool DefaultEnable => true;

    public override string Name => "弹弹play";

    public override string ProviderName => ScraperProviderName;

    public override string ProviderId => ScraperProviderId;

    public override async Task<string?> GetMatchMediaId(BaseItem item)
    {
        var searchName = this.NormalizeSearchName(item.Name);
        var animes = await this._api.SearchAsync(searchName, CancellationToken.None).ConfigureAwait(false);
        foreach (var anime in animes)
        {
            var animeId = anime.AnimeId;
            var title = anime.AnimeTitle;
            var pubYear = anime.Year;
            var isMovieItemType = item is MediaBrowser.Controller.Entities.Movies.Movie;

            if (isMovieItemType && anime.Type != "movie")
            {
                continue;
            }

            if (!isMovieItemType && anime.Type == "movie")
            {
                continue;
            }

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
                log.LogInformation("[{0}] 发行年份不一致，忽略处理. dandan：{1} jellyfin: {2}", title, pubYear, itemPubYear);
                continue;
            }

            return $"{animeId}";
        }

        return null;
    }


    public override async Task<ScraperMedia?> GetMedia(string id)
    {
        var animeId = id.ToLong();
        if (animeId <= 0)
        {
            return null;
        }

        var anime = await _api.GetAnimeAsync(animeId, CancellationToken.None).ConfigureAwait(false);
        if (anime == null)
        {
            log.LogInformation("[{0}]获取不到视频信息：id={1}", this.Name, animeId);
            return null;
        }

        var media = new ScraperMedia();
        media.Id = id;
        media.Name = anime.AnimeTitle;
        if (anime.Episodes != null && anime.Episodes.Count > 0)
        {
            foreach (var item in anime.Episodes)
            {
                media.Episodes.Add(new ScraperEpisode() { Id = $"{item.EpisodeId}", CommentId = $"{item.EpisodeId}" });
            }
        }

        return media;
    }

    public override async Task<ScraperEpisode?> GetMediaEpisode(string id)
    {
        var epId = id.ToLong();
        if (epId <= 0)
        {
            return null;
        }

        return new ScraperEpisode() { Id = id, CommentId = id };
    }

    public override async Task<ScraperDanmaku?> GetDanmuContent(string commentId)
    {
        var cid = commentId.ToLong();
        if (cid <= 0)
        {
            return null;
        }

        var comments = await _api.GetCommentsAsync(cid, CancellationToken.None).ConfigureAwait(false);
        var danmaku = new ScraperDanmaku();
        danmaku.ChatId = cid;
        danmaku.ChatServer = "api.dandanplay.net";
        foreach (var item in comments)
        {
            var danmakuText = new ScraperDanmakuText();
            var arr = item.P.Split(",");
            danmakuText.Progress = (int)(Convert.ToDouble(arr[0]) * 1000);
            danmakuText.Mode = Convert.ToInt32(arr[1]);
            danmakuText.Color = Convert.ToUInt32(arr[2]);
            danmakuText.MidHash = arr[3];
            danmakuText.Id = item.Cid;
            danmakuText.Content = item.Text;

            danmaku.Items.Add(danmakuText);

        }

        return danmaku;
    }


    private string NormalizeSearchName(string name)
    {
        // 去掉可能存在的季名称
        return Regex.Replace(name, @"\s*第.季", "");
    }
}
