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
using System.Text.Json;
using Jellyfin.Plugin.Danmu.Scrapers.Youku.Entity;

namespace Jellyfin.Plugin.Danmu.Scrapers.Youku;

public class Youku : AbstractScraper
{
    public const string ScraperProviderName = "优酷";
    public const string ScraperProviderId = "YoukuID";

    private readonly YoukuApi _api;

    public Youku(ILoggerFactory logManager)
        : base(logManager.CreateLogger<Youku>())
    {
        _api = new YoukuApi(logManager);
    }

    public override int DefaultOrder => 3;

    public override bool DefaultEnable => false;

    public override string Name => "优酷";

    public override string ProviderName => ScraperProviderName;

    public override string ProviderId => ScraperProviderId;

    public override async Task<string?> GetMatchMediaId(BaseItem item)
    {
        var searchName = this.NormalizeSearchName(item.Name);
        var videos = await this._api.SearchAsync(searchName, CancellationToken.None).ConfigureAwait(false);
        foreach (var video in videos)
        {
            var videoId = video.GroupID;
            var title = video.ObjectTitle;
            var pubYear = video.Year;
            var isMovieItemType = item is MediaBrowser.Controller.Entities.Movies.Movie;

            if (isMovieItemType && video.Type != "movie")
            {
                continue;
            }

            if (!isMovieItemType && video.Type == "movie")
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
                log.LogInformation("[{0}] 发行年份不一致，忽略处理. Youku：{1} jellyfin: {2}", title, pubYear, itemPubYear);
                continue;
            }

            return $"{videoId}";
        }

        return null;
    }


    public override async Task<ScraperMedia?> GetMedia(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        var video = await _api.GetVideoAsync(id, CancellationToken.None).ConfigureAwait(false);
        if (video == null)
        {
            log.LogInformation("[{0}]获取不到视频信息：id={1}", this.Name, id);
            return null;
        }

        var media = new ScraperMedia();
        media.Id = id;
        media.Name = "no name";
        if (video.Videos != null && video.Videos.Count > 0)
        {
            foreach (var item in video.Videos)
            {
                media.Episodes.Add(new ScraperEpisode() { Id = $"{item.ID}", CommentId = $"{item.ID}" });
            }
        }

        return media;
    }

    public override async Task<ScraperEpisode?> GetMediaEpisode(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        return new ScraperEpisode() { Id = id, CommentId = id };
    }

    public override async Task<ScraperDanmaku?> GetDanmuContent(string commentId)
    {
        if (string.IsNullOrEmpty(commentId))
        {
            return null;
        }

        var comments = await _api.GetDanmuContentAsync(commentId, CancellationToken.None).ConfigureAwait(false);
        var danmaku = new ScraperDanmaku();
        danmaku.ChatId = 1000;
        danmaku.ChatServer = "acs.youku.com";
        foreach (var item in comments)
        {
            try
            {
                var danmakuText = new ScraperDanmakuText();
                danmakuText.Progress = (int)item.Playat;
                danmakuText.Mode = 1;
                danmakuText.MidHash = item.Uid;
                danmakuText.Id = item.ID;
                danmakuText.Content = item.Content;

                var property = JsonSerializer.Deserialize<YoukuCommentProperty>(item.Propertis);
                if (property != null)
                {
                    danmakuText.Color = property.Color;
                }

                danmaku.Items.Add(danmakuText);
            }
            catch (Exception ex)
            {

            }

        }

        return danmaku;
    }


    private string NormalizeSearchName(string name)
    {
        // 去掉可能存在的季名称
        return Regex.Replace(name, @"\s*第.季", "");
    }
}
