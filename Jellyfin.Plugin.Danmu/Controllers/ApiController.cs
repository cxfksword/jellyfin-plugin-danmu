using System;
using System.IO;
using System.Globalization;
using System.Threading.Tasks;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediaBrowser.Model.IO;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Dto;
using System.Collections.Generic;
using Jellyfin.Plugin.Danmu.Scrapers;
using Microsoft.Extensions.Logging;
using Jellyfin.Plugin.Danmu.Core.Extensions;
using System.Text.RegularExpressions;
using System.Linq;
using Jellyfin.Plugin.Danmu.Scrapers.Dandan.Entity;
using Jellyfin.Plugin.Danmu.Controllers.Entity;
using Jellyfin.Plugin.Danmu.Core;
using Microsoft.Extensions.Caching.Memory;

namespace Jellyfin.Plugin.Danmu.Controllers
{
    [ApiController]
    [AllowAnonymous]
    public class ApiController : ControllerBase
    {
        private readonly ScraperManager _scraperManager;
        private static TimeSpan _cacheExpiration = TimeSpan.FromDays(1);
        private static readonly IMemoryCache _animeIdCache = new MemoryCache(new MemoryCacheOptions
            {
                // 设置缓存大小限制（可选）
                SizeLimit = 10000
            });
        private static readonly IMemoryCache _episodeIdCache = new MemoryCache(new MemoryCacheOptions
            {
                // 设置缓存大小限制（可选）
                SizeLimit = 10000
            });

        private readonly FileCache<AnimeCacheItem> _animeFileCache;

        private readonly ILogger<ApiController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiController"/> class.
        /// </summary>
        /// <param name="loggerFactory">Instance of the <see cref="ILoggerFactory"/> interface.</param>
        /// <param name="scraperManager">Instance of the <see cref="ScraperManager"/> class.</param>
        /// <param name="animeFileCache">File-backed cache injected via dependency injection.</param>
        public ApiController(
            ILoggerFactory loggerFactory,
            ScraperManager scraperManager,
            FileCache<AnimeCacheItem> animeFileCache)
        {
            _logger = loggerFactory.CreateLogger<ApiController>();
            _scraperManager = scraperManager;
            _animeFileCache = animeFileCache ?? throw new ArgumentNullException(nameof(animeFileCache));
        }


        /// <summary>
        /// 查找弹幕
        /// </summary>
        [Route("/api/v2/search/anime")]
        [Route("/{token}/api/v2/search/anime")]
        [HttpGet]
        public async Task<ApiResult<Anime>> SearchAnime(string keyword)
        {
            var list = new List<Anime>();

            if (string.IsNullOrEmpty(keyword))
            {
                return new ApiResult<Anime>()
                {
                    ErrorCode = 400,
                    Success = false,
                    ErrorMessage = "Keyword cannot be empty",
                    Animes = list
                };
            }

            var scrapers = this._scraperManager.All();
            var searchTasks = scrapers.Select(async scraper =>
            {
                try
                {
                    var result = await scraper.SearchForApi(keyword).ConfigureAwait(false);
                    return result.Select(searchInfo => 
                    {
                        var animeId = ConvertToHashId(scraper, searchInfo.Id);
                        var anime = new Anime()
                        {
                            AnimeId = animeId,
                            BangumiId = $"{animeId}",
                            AnimeTitle = $"{searchInfo.Name} from {scraper.Name}",
                            ImageUrl = "https://dummyimage.com/300x450/fff/ccc&text=No+Image",
                            Type = searchInfo.Category,
                            TypeDescription = searchInfo.Category,
                            StartDate = searchInfo.StartDate,
                            EpisodeCount = searchInfo.EpisodeSize > 0 ? searchInfo.EpisodeSize : null,
                            Episodes = null
                        };

                        var cacheOptions = new MemoryCacheEntryOptions
                        {
                            Size = 1,
                            AbsoluteExpirationRelativeToNow = _cacheExpiration,
                        };
                        _animeIdCache.Set(anime.AnimeId, new AnimeCacheItem(scraper.ProviderId, searchInfo.Id, anime), cacheOptions);
                        return anime;
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[{0}]Exception handled processing search movie [{1}]", scraper.Name, keyword);
                    return Enumerable.Empty<Anime>();
                }
            });

            var results = await Task.WhenAll(searchTasks).ConfigureAwait(false);
            
            foreach (var result in results)
            {
                list.AddRange(result);
            }

            return new ApiResult<Anime>()
            {
                Success = true,
                Animes = list
            };
        }

        /// <summary>
        /// 根据关键词搜索所有匹配的剧集信息
        /// </summary>
        [Route("/api/v2/search/episodes")]
        [Route("/{token}/api/v2/search/episodes")]
        [HttpGet]
        public async Task<ApiResult<Anime>> SearchAnimeEpisodes(string anime)
        {
            var list = new List<Anime>();

            if (string.IsNullOrEmpty(anime))
            {
                return new ApiResult<Anime>()
                {
                    ErrorCode = 400,
                    Success = false,
                    ErrorMessage = "Anime cannot be empty",
                    Animes = list
                };
            }

            
            var scrapers = this._scraperManager.All();
            var searchTasks = scrapers.Select(async scraper =>
            {
                try
                {
                    var result = await scraper.SearchForApi(anime).ConfigureAwait(false);
                    return result.Select(searchInfo => 
                    {
                        var animeId = ConvertToHashId(scraper, searchInfo.Id);
                        var animeObj = new Anime()
                        {
                            AnimeId = animeId,
                            BangumiId = $"{animeId}",
                            AnimeTitle = $"{searchInfo.Name} from {scraper.Name}",
                            ImageUrl = "https://dummyimage.com/300x450/fff/ccc&text=No+Image",
                            Type = searchInfo.Category,
                            TypeDescription = searchInfo.Category,
                            StartDate = searchInfo.StartDate,
                            EpisodeCount = searchInfo.EpisodeSize > 0 ? searchInfo.EpisodeSize : null,
                            Episodes = this.GetAnimeEpisodes(scraper, searchInfo.Id).GetAwaiter().GetResult(),
                        };

                        var cacheOptions = new MemoryCacheEntryOptions
                        {
                            Size = 1,
                            AbsoluteExpirationRelativeToNow = _cacheExpiration,
                        };
                        _animeIdCache.Set(animeObj.AnimeId, new AnimeCacheItem(scraper.ProviderId, searchInfo.Id, animeObj), cacheOptions);
                        return animeObj;
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[{0}]Exception handled processing search movie [{1}]", scraper.Name, anime);
                    return Enumerable.Empty<Anime>();
                }
            });

            var results = await Task.WhenAll(searchTasks).ConfigureAwait(false);
            
            foreach (var result in results)
            {
                list.AddRange(result);
            }

            return new ApiResult<Anime>()
            {
                Success = true,
                Animes = list
            };
        }

        /// <summary>
        /// 获取详细信息
        /// </summary>
        [Route("/api/v2/bangumi/{id}")]
        [Route("/{token}/api/v2/bangumi/{id}")]
        [HttpGet]
        public async Task<ApiResult<Anime>> GetAnime(string id)
        {
            var list = new List<EpisodeInfo>();

            if (string.IsNullOrEmpty(id))
            {
                return new ApiResult<Anime>()
                {
                    ErrorCode = 400,
                    Success = false,
                    ErrorMessage = "ID cannot be empty",
                };
            }

            if (!long.TryParse(id, out var animeId))
            {
                return new ApiResult<Anime>()
                {
                    ErrorCode = 400,
                    Success = false,
                    ErrorMessage = "ID format is invalid",
                };
            }

            // 从缓存中获取Anime数据
            if (!_animeIdCache.TryGetValue(animeId, out AnimeCacheItem? animeCacheItem) || animeCacheItem == null)
            {
                if (this._animeFileCache.TryGetValue(animeId.ToString(CultureInfo.InvariantCulture), out animeCacheItem) && animeCacheItem != null)
                {
                    // 将数据重新放入内存缓存
                    var cacheOptions = new MemoryCacheEntryOptions
                    {
                        Size = 1,
                        AbsoluteExpirationRelativeToNow = _cacheExpiration,
                    };
                    _animeIdCache.Set(animeId, animeCacheItem, cacheOptions);
                }
                else
                {
                    return new ApiResult<Anime>()
                    {
                        ErrorCode = 404,
                        Success = false,
                        ErrorMessage = "Anime not found",
                    };
                }
            }

            // 延长文件缓存时间或写入文件缓存
            this._animeFileCache.Set(animeId.ToString(CultureInfo.InvariantCulture), animeCacheItem);

            var animeData = animeCacheItem.AnimeData;
            if (animeData == null)
            {
                return new ApiResult<Anime>()
                {
                    ErrorCode = 404,
                    Success = false,
                    ErrorMessage = "Anime not found",
                };
            }

            var scraper = this._scraperManager.All().FirstOrDefault(s => s.ProviderId == animeCacheItem.ScraperProviderId);
            if (scraper == null)
            {
                return new ApiResult<Anime>()
                {
                    ErrorCode = 400,
                    Success = false,
                    ErrorMessage = "No scraper available",
                };
            }
 
            try
            {
                var anime = animeData;
                anime.Episodes = await this.GetAnimeEpisodes(scraper, animeCacheItem.Id).ConfigureAwait(false);
                anime.EpisodeCount = anime.Episodes?.Count;
                
                return new ApiResult<Anime>()
                {
                    Success = true,
                    Bangumi = anime
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{0}]Exception handled processing get episodes [{1}]", scraper.Name, id);
                return new ApiResult<Anime>()
                {
                    ErrorCode = 500,
                    Success = false,
                    ErrorMessage = ex.Message,
                };
            }
        }


        private async Task<List<Jellyfin.Plugin.Danmu.Scrapers.Dandan.Entity.Episode>> GetAnimeEpisodes(AbstractScraper scraper, string id)
        {
            var list = new List<Jellyfin.Plugin.Danmu.Scrapers.Dandan.Entity.Episode>();
            try
            {
                var result = await scraper.GetEpisodesForApi(id).ConfigureAwait(false);
                foreach (var (ep, idx) in result.WithIndex())
                {
                    var episode = new Jellyfin.Plugin.Danmu.Scrapers.Dandan.Entity.Episode()
                    {
                        EpisodeId = ConvertToHashId(scraper, ep.Id),
                        EpisodeNumber = $"{idx + 1}",
                        EpisodeTitle = ep.Title,
                        AirDate = "1970-01-01T00:00:00Z"
                    };

                    var cacheOptions = new MemoryCacheEntryOptions
                    {
                        Size = 1,
                        AbsoluteExpirationRelativeToNow = _cacheExpiration,
                    };
                    _episodeIdCache.Set(episode.EpisodeId, new CommentCacheItem(scraper.ProviderId, ep.CommentId), cacheOptions);

                    list.Add(episode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{0}]Exception handled processing get episodes [{1}]", scraper.Name, id);
            }

            return list;
        }


        /// <summary>
        /// 下载弹幕.
        /// </summary>
        [Route("/api/v2/comment/{cid}")]
        [Route("/{token}/api/v2/comment/{cid}")]
        [HttpGet]
        public async Task<ActionResult> DownloadByCommentID(string cid, int chConvert=0, bool withRelated=true, string format="json")
        {
            if (string.IsNullOrEmpty(cid))
            {
                throw new ResourceNotFoundException();
            }

            // 将字符串 cid 转换为 long 类型
            if (!long.TryParse(cid, out var episodeId))
            {
                throw new ResourceNotFoundException();
            }

            // 从缓存中获取episode数据
            if (!_episodeIdCache.TryGetValue(episodeId, out CommentCacheItem? commentCacheItem) || commentCacheItem == null)
            {
                throw new ResourceNotFoundException();
            }

            // 延长缓存时间
            var cacheOptions = new MemoryCacheEntryOptions
            {
                Size = 1,
                AbsoluteExpirationRelativeToNow = _cacheExpiration,
            };
            _episodeIdCache.Set(episodeId, commentCacheItem, cacheOptions);
            
            var scraper = this._scraperManager.All().FirstOrDefault(s => s.ProviderId == commentCacheItem.ScraperProviderId);
            if (scraper == null)
            {
                throw new ResourceNotFoundException();
            }

            var commentId = commentCacheItem.CommentId;
            var danmaku = await scraper.DownloadDanmuForApi(commentId).ConfigureAwait(false);
            if (danmaku != null)
            {
                if (format.ToLower() == "xml")
                {
                    var bytes = danmaku.ToXml();
                    return File(bytes, "text/xml");
                }
                else
                {
                    // 把 danmaku 转为 CommentResult
                    var result = new Jellyfin.Plugin.Danmu.Scrapers.Dandan.Entity.CommentResult
                    {
                        Comments = danmaku.Items.Select(item => new Jellyfin.Plugin.Danmu.Scrapers.Dandan.Entity.Comment
                        {
                            Cid = item.Id,
                            P = $"{item.Progress / 1000.0:F2},{item.Mode},{item.Color},{item.MidHash}",
                            Text = item.Content,
                        }).ToList()
                    };

                    var jsonString = result.ToJson();
                    return File(System.Text.Encoding.UTF8.GetBytes(jsonString), "application/json");
                }
            }

            throw new ResourceNotFoundException();
        }


        /// <summary>
        /// 将 scraper ID 转换为带有 HashPrefix 的 11 位 long 类型 AnimeId
        /// 格式：前 2 位是 HashPrefix，后 9 位是 ID 的哈希值
        /// 例如：HashPrefix=10, Id="abc123" => 10xxxxxxxxx (后9位是哈希值)
        /// </summary>
        /// <param name="scraper">弹幕源</param>
        /// <param name="id">原始 ID 字符串（可以是任意字母数字组合）</param>
        /// <param name="animeData">完整的 Anime 数据（可选）</param>
        /// <returns>11 位 long 类型的 AnimeId，失败时返回 0</returns>
        private long ConvertToHashId(AbstractScraper scraper, string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return 0;
            }

            long animeId;

            // 如果是纯数字且不超过 9 位，直接使用
            if (long.TryParse(id, out var numericId) && numericId <= 999999999)
            {
                animeId = ((long)scraper.HashPrefix * 1000000000) + numericId;
            }
            else
            {
                // 对于非纯数字或超长的 ID，使用哈希算法转换为 9 位数字
                // 使用 GetHashCode 并取绝对值，然后模 999999999 确保在 9 位范围内
                var hashCode = id.GetHashCode();
                var hashValue = Math.Abs(hashCode) % 1000000000; // 确保在 0-999999999 范围内
                
                animeId = ((long)scraper.HashPrefix * 1000000000) + hashValue;
            }

            return animeId;
        }
    }
}
