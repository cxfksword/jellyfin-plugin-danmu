using System.Web;
using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using System.Collections.Generic;
using Emby.Plugin.Danmu.Core.Extensions;
using Emby.Plugin.Danmu.Core.Singleton;
using Emby.Plugin.Danmu.Scraper.Entity;
using Emby.Plugin.Danmu.Scraper.Youku.Entity;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Logging;

namespace Emby.Plugin.Danmu.Scraper.Youku
{
    public class Youku : AbstractScraper
    {
        public const string ScraperProviderName = "优酷";
        public const string ScraperProviderId = "YoukuID";

        private readonly YoukuApi _api;

        public Youku(ILogManager logManager, IHttpClient httpClient)
            : base(logManager.getDefaultLogger("Tencent"))
        {
            _api = new YoukuApi(logManager, httpClient);
        }

        public override int DefaultOrder => 3;

        public override bool DefaultEnable => true;

        public override string Name => "优酷";

        public override string ProviderName => ScraperProviderName;

        public override string ProviderId => ScraperProviderId;

        public override async Task<List<ScraperSearchInfo>> Search(BaseItem item)
        {
            var list = new List<ScraperSearchInfo>();
            var isMovieItemType = item is MediaBrowser.Controller.Entities.Movies.Movie;
            var searchName = this.NormalizeSearchName(item.Name);
            var videos = await this._api.SearchAsync(searchName, CancellationToken.None).ConfigureAwait(false);
            foreach (var video in videos)
            {
                var videoId = video.ID;
                var title = video.Title;
                var pubYear = video.Year;

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
                    continue;
                }

                list.Add(new ScraperSearchInfo()
                {
                    Id = $"{videoId}",
                    Name = title,
                    Category = video.Type == "movie" ? "电影" : "电视剧",
                    Year = pubYear,
                    EpisodeSize = video.Total,
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
                var videoId = video.ID;
                var title = video.Title;
                var pubYear = video.Year;

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
                    log.Info("[{0}] 标题差异太大，忽略处理. 搜索词：{1}, score:　{2}", title, searchName, score);
                    continue;
                }

                // 检测年份是否一致
                var itemPubYear = item.ProductionYear ?? 0;
                if (itemPubYear > 0 && pubYear > 0 && itemPubYear != pubYear)
                {
                    log.Info("[{0}] 发行年份不一致，忽略处理. Youku：{1} emby: {2}", title, pubYear, itemPubYear);
                    continue;
                }

                return $"{videoId}";
            }

            return null;
        }


        public override async Task<ScraperMedia?> GetMedia(BaseItem item, string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            id = HttpUtility.UrlDecode(id);
            var video = await _api.GetVideoAsync(id, CancellationToken.None).ConfigureAwait(false);
            if (video == null)
            {
                log.LogInformation("[{0}]获取不到视频信息：id={1}", this.Name, id);
                return null;
            }

            var isMovieItemType = item is MediaBrowser.Controller.Entities.Movies.Movie;
            var media = new ScraperMedia();
            if (video.Videos != null && video.Videos.Count > 0)
            {
                foreach (var ep in video.Videos)
                {
                    media.Episodes.Add(new ScraperEpisode() { Id = $"{ep.ID}", CommentId = $"{ep.ID}" });
                }
            }

            // 优酷的id包括非法的=符号，会导致jellyfin自动删除，这里做下encode
            if (isMovieItemType)
            {
                media.Id = HttpUtility.UrlEncode(media.Episodes.Count > 0 ? $"{media.Episodes[0].Id}" : "");
                media.CommentId = media.Episodes.Count > 0 ? $"{media.Episodes[0].CommentId}" : "";
            }
            else
            {
                media.Id = HttpUtility.UrlEncode(id);
            }

            return media;
        }

        public override async Task<ScraperEpisode?> GetMediaEpisode(BaseItem item, string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            id = HttpUtility.UrlDecode(id);
            return new ScraperEpisode() { Id = id, CommentId = id };
        }

        public override async Task<ScraperDanmaku?> GetDanmuContent(BaseItem item, string commentId)
        {
            if (string.IsNullOrEmpty(commentId))
            {
                return null;
            }

            var comments = await _api.GetDanmuContentAsync(commentId, CancellationToken.None).ConfigureAwait(false);
            var danmaku = new ScraperDanmaku();
            danmaku.ChatId = 1000;
            danmaku.ChatServer = "acs.youku.com";
            danmaku.ProviderId = ProviderId;
            foreach (var comment in comments)
            {
                try
                {
                    var danmakuText = new ScraperDanmakuText();
                    danmakuText.Progress = (int)comment.Playat;
                    danmakuText.Mode = 1;
                    danmakuText.MidHash = $"[youku]{comment.Uid}";
                    danmakuText.Id = comment.ID;
                    danmakuText.Content = comment.Content;

                    var property = SingletonManager.JsonSerializer.DeserializeFromString<YoukuCommentProperty>(comment.Propertis);
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


        public override async Task<List<ScraperSearchInfo>> SearchForApi(string keyword)
        {
            var list = new List<ScraperSearchInfo>();
            var videos = await this._api.SearchAsync(keyword, CancellationToken.None).ConfigureAwait(false);
            foreach (var video in videos)
            {
                var videoId = video.ID;
                var title = video.Title;
                var pubYear = video.Year;

                var score = keyword.Distance(title);
                if (score <= 0)
                {
                    continue;
                }

                list.Add(new ScraperSearchInfo()
                {
                    Id = $"{videoId}",
                    Name = title,
                    Category = video.Type == "movie" ? "电影" : "电视剧",
                    Year = pubYear,
                    EpisodeSize = video.Total,
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

            if (video.Videos != null && video.Videos.Count > 0)
            {
                foreach (var ep in video.Videos)
                {
                    list.Add(new ScraperEpisode() { Id = $"{ep.ID}", CommentId = $"{ep.ID}", Title = ep.Title });
                }
            }

            return list;
        }

        public override async Task<ScraperDanmaku?> DownloadDanmuForApi(string commentId)
        {
            return await this.GetDanmuContent(null, commentId).ConfigureAwait(false);
        }
    }
}