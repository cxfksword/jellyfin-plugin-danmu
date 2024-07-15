using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Emby.Plugin.Danmu.Core.Extensions;
using Emby.Plugin.Danmu.Scraper.Entity;
using Emby.Plugin.Danmu.Scraper.Tencent.Entity;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Logging;

namespace Emby.Plugin.Danmu.Scraper.Tencent
{
    public class Tencent : AbstractScraper
    {
        public const string ScraperProviderName = "腾讯";
        public const string ScraperProviderId = "TencentID";

        private readonly TencentApi _api;

        public Tencent(ILogManager logManager, IHttpClient httpClient)
            : base(logManager.getDefaultLogger("Tencent"))
        {
            _api = new TencentApi(logManager, httpClient);
        }

        public override int DefaultOrder => 5;

        public override bool DefaultEnable => true;

        public override string Name => ScraperProviderName;

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
                var videoId = video.Id;
                var title = video.Title;
                var pubYear = video.Year;
                var episodeSize = video.SubjectDoc.VideoNum;

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
                    EpisodeSize = episodeSize,
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
                media.CommentId = $"{video.EpisodeList[0].Vid}";
            }

            if (video.EpisodeList != null && video.EpisodeList.Count > 0)
            {
                foreach (var ep in video.EpisodeList)
                {
                    media.Episodes.Add(new ScraperEpisode() { Id = $"{ep.Vid}", CommentId = $"{ep.Vid}" });
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

                return new ScraperEpisode() { Id = id, CommentId = $"{video.EpisodeList[0].Vid}" };
            }

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
            danmaku.ChatServer = "dm.video.qq.com";
            danmaku.ProviderId = ProviderId;
            foreach (var comment in comments)
            {
                try
                {
                    var midHash = string.IsNullOrEmpty(comment.Nick) ? "anonymous".ToBase64() : comment.Nick.ToBase64();
                    var danmakuText = new ScraperDanmakuText();
                    danmakuText.Progress = comment.TimeOffset.ToInt();
                    danmakuText.Mode = 1;
                    danmakuText.MidHash = $"[tencent]{midHash}";
                    danmakuText.Id = comment.Id.ToLong();
                    danmakuText.Content = comment.Content.Replace("VIP :", "");
                    if (!string.IsNullOrEmpty(comment.ContentStyle))
                    {
                        var style = comment.ContentStyle.FromJson<TencentCommentContentStyle>();
                        if (style != null && uint.TryParse(style.Color, System.Globalization.NumberStyles.HexNumber,
                                null, out var color))
                        {
                            danmakuText.Color = color;
                        }

                        if (style != null && style.Position > 0)
                        {
                            switch (style.Position)
                            {
                                case 2: // top
                                    danmakuText.Mode = 5;
                                    break;
                                case 3: // bottom
                                    danmakuText.Mode = 4;
                                    break;
                            }
                        }
                    }

                    danmaku.Items.Add(danmakuText);
                }
                catch (Exception ex)
                {
                }
            }
            
            danmaku.DataSize = danmaku.Items.Count;
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
                var episodeSize = video.SubjectDoc.VideoNum;
                list.Add(new ScraperSearchInfo()
                {
                    Id = $"{videoId}",
                    Name = title,
                    Category = video.TypeName,
                    Year = pubYear,
                    EpisodeSize = episodeSize,
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
                    list.Add(new ScraperEpisode() { Id = $"{ep.Vid}", CommentId = $"{ep.Vid}", Title = ep.Title });
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