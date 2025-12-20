using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using Microsoft.Extensions.Logging;
using Jellyfin.Plugin.Danmu.Scrapers.Entity;
using Jellyfin.Plugin.Danmu.Core.Extensions;
using System.Text.RegularExpressions;
using Jellyfin.Plugin.Danmu.Core;

namespace Jellyfin.Plugin.Danmu.Scrapers.DanmuApi;

public class DanmuApi : AbstractScraper
{
    public const string ScraperProviderName = "弹幕API";
    public const string ScraperProviderId = "DanmuApiID";

    private readonly DanmuApiApi _api;

    public DanmuApi(ILoggerFactory logManager)
        : base(logManager.CreateLogger<DanmuApi>())
    {
        _api = new DanmuApiApi(logManager);
    }

    public override int DefaultOrder => 10;

    public override bool DefaultEnable => false;

    public override string Name => "弹幕API";

    public override string ProviderName => ScraperProviderName;

    public override string ProviderId => ScraperProviderId;

    public override uint HashPrefix => 16;

    public override async Task<List<ScraperSearchInfo>> Search(BaseItem item)
    {
        var list = new List<ScraperSearchInfo>();
        var isMovieItemType = item is MediaBrowser.Controller.Entities.Movies.Movie;
        var searchName = this.NormalizeSearchName(item.Name);
        var animes = await this._api.SearchAsync(searchName, CancellationToken.None).ConfigureAwait(false);

        // 过滤采集源
        animes = FilterByAllowedSources(animes);

        foreach (var anime in animes)
        {
            list.Add(new ScraperSearchInfo()
            {
                Id = anime.BangumiId,
                Name = this.NormalizeAnimeTitle(anime.AnimeTitle),
                Category = anime.TypeDescription,
                Year = anime.Year,
                EpisodeSize = anime.EpisodeCount,
            });
        }

        return list;
    }

    public override async Task<string?> SearchMediaId(BaseItem item)
    {
        var isMovieItemType = item is MediaBrowser.Controller.Entities.Movies.Movie;
        var searchName = this.NormalizeSearchName(item.Name);
        var animes = await this._api.SearchAsync(searchName, CancellationToken.None).ConfigureAwait(false);
        
        // 过滤采集源
        animes = FilterByAllowedSources(animes);
        
        foreach (var anime in animes)
        {
            var title = this.NormalizeSearchName(anime.AnimeTitle);

            // 检测标题是否相似（越大越相似）
            var score = searchName.Distance(title);
            if (score < 0.7)
            {
                log.LogDebug("[{0}] 标题差异太大，忽略处理. 搜索词：{1}, score: {2}", title, searchName, score);
                continue;
            }

            // 检测年份是否一致
            var itemPubYear = item.ProductionYear ?? 0;
            if (itemPubYear > 0 && anime.Year > 0 && itemPubYear != anime.Year)
            {
                log.LogDebug("[{0}] 发行年份不一致，忽略处理. 年份：{1} jellyfin: {2}", title, anime.Year, itemPubYear);
                continue;
            }

            return anime.BangumiId;
        }

        return null;
    }

    public override async Task<ScraperMedia?> GetMedia(BaseItem item, string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        var bangumi = await _api.GetBangumiAsync(id, CancellationToken.None).ConfigureAwait(false);
        if (bangumi == null)
        {
            log.LogInformation("[{0}]获取不到视频信息：id={1}", this.Name, id);
            return null;
        }

        // 过滤平台源
        var filteredEpisodes = FilterByAllowedPlatforms(bangumi);

        var isMovieItemType = item is MediaBrowser.Controller.Entities.Movies.Movie;
        var media = new ScraperMedia();

        media.Id = id;
        if (isMovieItemType && filteredEpisodes.Count > 0)
        {
            media.CommentId = filteredEpisodes[0].EpisodeId;
        }
        
        if (filteredEpisodes.Count > 0)
        {
            foreach (var ep in filteredEpisodes)
            {
                media.Episodes.Add(new ScraperEpisode() 
                { 
                    Id = ep.EpisodeId, 
                    CommentId = ep.EpisodeId,
                    Title = ep.EpisodeTitle
                });
            }
        }

        return media;
    }

    public override async Task<ScraperEpisode?> GetMediaEpisode(BaseItem item, string id)
    {
        var isMovieItemType = item is MediaBrowser.Controller.Entities.Movies.Movie;
        if (isMovieItemType)
        {
            // id 是 bangumiId
            var bangumi = await _api.GetBangumiAsync(id, CancellationToken.None).ConfigureAwait(false);
            if (bangumi == null || bangumi.Episodes == null)
            {
                return null;
            }

            // 过滤平台源
            var filteredEpisodes = FilterByAllowedPlatforms(bangumi);
            if (filteredEpisodes.Count == 0)
            {
                return null;
            }

            return new ScraperEpisode() 
            { 
                Id = id, 
                CommentId = filteredEpisodes[0].EpisodeId,
                Title = filteredEpisodes[0].EpisodeTitle
            };
        }
        else
        {
            // id 是 episodeId
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            return new ScraperEpisode() { Id = id, CommentId = id };
        }
    }

    public override async Task<ScraperDanmaku?> GetDanmuContent(BaseItem item, string commentId)
    {
        if (string.IsNullOrEmpty(commentId))
        {
            return null;
        }

        var comments = await _api.GetCommentsAsync(commentId, CancellationToken.None).ConfigureAwait(false);
        var danmaku = new ScraperDanmaku();
        danmaku.ChatId = 0;
        danmaku.ChatServer = "danmu-api";
        
        foreach (var comment in comments)
        {
            var danmakuText = new ScraperDanmakuText();
            var arr = comment.P.Split(",");
            
            if (arr.Length >= 4)
            {
                danmakuText.Progress = (int)(Convert.ToDouble(arr[0]) * 1000);
                danmakuText.Mode = Convert.ToInt32(arr[1]);
                danmakuText.Color = Convert.ToUInt32(arr[2]);
                danmakuText.MidHash = arr[3];
                danmakuText.Id = comment.Cid;
                danmakuText.Content = comment.M;

                danmaku.Items.Add(danmakuText);
            }
        }

        return danmaku;
    }

    public override async Task<List<ScraperSearchInfo>> SearchForApi(string keyword)
    {
        var list = new List<ScraperSearchInfo>();
        var animes = await this._api.SearchAsync(keyword, CancellationToken.None).ConfigureAwait(false);
        
        // 过滤采集源
        animes = FilterByAllowedSources(animes);
        
        foreach (var anime in animes)
        {
            list.Add(new ScraperSearchInfo()
            {
                Id = anime.BangumiId,
                Name = anime.AnimeTitle,
                Category = anime.TypeDescription,
                Year = anime.Year,
                EpisodeSize = anime.EpisodeCount,
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

        var bangumi = await this._api.GetBangumiAsync(id, CancellationToken.None).ConfigureAwait(false);
        if (bangumi == null)
        {
            return list;
        }

        // 过滤平台源
        var filteredEpisodes = FilterByAllowedPlatforms(bangumi);

        if (filteredEpisodes.Count > 0)
        {
            foreach (var ep in filteredEpisodes)
            {
                list.Add(new ScraperEpisode() 
                { 
                    Id = ep.EpisodeId, 
                    CommentId = ep.EpisodeId, 
                    Title = ep.EpisodeTitle 
                });
            }
        }

        return list;
    }

    public override async Task<ScraperDanmaku?> DownloadDanmuForApi(string commentId)
    {
        return await this.GetDanmuContent(null, commentId).ConfigureAwait(false);
    }

    /// <summary>
    /// 根据配置的允许采集源列表过滤结果
    /// </summary>
    private List<Entity.Anime> FilterByAllowedSources(List<Entity.Anime> animes)
    {
        var allowedSources = Plugin.Instance?.Configuration?.DanmuApi?.AllowedSources;
        
        // 如果未配置采集源限制，返回所有结果
        if (string.IsNullOrWhiteSpace(allowedSources))
        {
            return animes;
        }

        // 解析允许的采集源列表（按逗号分隔，转小写）
        var sourceSet = allowedSources
            .Split(',')
            .Select(s => s.Trim().ToLowerInvariant())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToHashSet();

        // 如果采集源列表为空，返回所有结果
        if (sourceSet.Count == 0)
        {
            return animes;
        }

        // 过滤：只保留 Source 属性在允许列表中的结果
        var filtered = animes.Where(anime => 
        {
            var source = anime.Source.ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(source))
            {
                // 没有 source 标记不保留
                return false;
            }
            return sourceSet.Contains(source);
        }).ToList();

        return filtered;
    }


    /// <summary>
    /// 根据配置的允许平台列表过滤结果
    /// </summary>
    private List<Entity.Episode> FilterByAllowedPlatforms(Entity.Bangumi bangumi)
    {
        var allowedPlatforms = Plugin.Instance?.Configuration?.DanmuApi?.AllowedPlatforms;
        
        // 如果未配置平台限制，返回第一组剧集
        if (string.IsNullOrWhiteSpace(allowedPlatforms))
        {
            return bangumi.EpisodeGroups.FirstOrDefault()?.Episodes ?? new List<Entity.Episode>();
        }

        // 解析允许的平台列表（按逗号分隔，转小写）
        var platformSet = allowedPlatforms
            .Split(',')
            .Select(p => p.Trim().ToLowerInvariant())
            .Where(p => !string.IsNullOrEmpty(p))
            .ToHashSet();

        // 如果平台列表为空，返回第一组剧集
        if (platformSet.Count == 0)
        {
            return bangumi.EpisodeGroups.FirstOrDefault()?.Episodes ?? new List<Entity.Episode>();
        }

        // 从分组中找到第一个匹配的平台组
        var groups = bangumi.EpisodeGroups;
        foreach (var group in groups)
        {
            var platform = group.Platform.ToLowerInvariant() ?? string.Empty;
            
            // 如果平台为空（未知平台）且只有一个组，返回该组
            if (string.IsNullOrWhiteSpace(platform) && groups.Count == 1)
            {
                return group.Episodes;
            }
            
            // 如果平台在允许列表中，返回该组
            if (platformSet.Contains(platform))
            {
                return group.Episodes;
            }
        }

        // 如果没有匹配的平台组，返回空列表
        return new List<Entity.Episode>();
    }

    protected override string NormalizeSearchName(string name)
    {
        return Utils.NormalizeSearchName(name);
    }

    protected string NormalizeAnimeTitle(string name)
    {
        return Regex.Replace(name, @"\(\d{4}\)|【.*】", " ").Trim();
    }
}
