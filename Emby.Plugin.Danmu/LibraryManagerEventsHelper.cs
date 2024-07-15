using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Plugin.Danmu.Configuration;
using Emby.Plugin.Danmu.Core;
using Emby.Plugin.Danmu.Core.Extensions;
using Emby.Plugin.Danmu.Core.Singleton;
using Emby.Plugin.Danmu.Model;
using Emby.Plugin.Danmu.Scraper;
using Emby.Plugin.Danmu.Scraper.Entity;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using Microsoft.Extensions.Caching.Memory;
using IFileSystem = Emby.Plugin.Danmu.Core.IFileSystem;

namespace Emby.Plugin.Danmu
{
    public class LibraryManagerEventsHelper : IDisposable
    {
        private readonly List<LibraryEvent> _queuedEvents;
        private readonly IMemoryCache _memoryCache;

        private bool ignoreEpisodesMatch = true;

        private readonly MemoryCacheEntryOptions _pendingAddExpiredOption = new MemoryCacheEntryOptions()
            { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) };

        private readonly MemoryCacheEntryOptions _danmuUpdatedExpiredOption = new MemoryCacheEntryOptions()
            { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) };

        private readonly ILibraryManager _libraryManager;
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;
        private Timer _queueTimer;
        private readonly ScraperManager _scraperManager;

        public PluginConfiguration Config
        {
            get { return Plugin.Instance?.Configuration ?? new Configuration.PluginConfiguration(); }
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="LibraryManagerEventsHelper"/> class.
        /// </summary>
        /// <param name="libraryManager">The <see cref="ILibraryManager"/>.</param>
        /// <param name="api">The <see cref="BilibiliApi"/>.</param>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        /// <param name="logManager"></param>
        /// <param name="scraperManager"></param>
        public LibraryManagerEventsHelper(ILibraryManager libraryManager, ILogManager logManager)
        {
            _queuedEvents = new List<LibraryEvent>();
            _memoryCache = new MemoryCache(new MemoryCacheOptions());

            _libraryManager = libraryManager;
            // 新增媒体信息
            // _libraryManager.ItemAdded += (sender, args) =>
            // {
            //     var argsItem = args.Item;
            //     QueueItem(argsItem, EventType.Add);
            //     QueueItem(argsItem, EventType.Update);
            //     QueueItem(argsItem, EventType.Update);
            // };
            _logger = logManager.getDefaultLogger(GetType().ToString());
            _scraperManager = SingletonManager.ScraperManager;
            _fileSystem = FileSystem.instant;
        }

        /// <summary>
        /// Queues an item to be added to trakt.
        /// </summary>
        /// <param name="item"> The <see cref="BaseItem"/>.</param>
        /// <param name="eventType">The <see cref="EventType"/>.</param>
        public void QueueItem(BaseItem item, EventType eventType)
        {
            lock (_queuedEvents)
            {
                if (item == null)
                {
                    throw new ArgumentNullException(nameof(item));
                }

                if (_queueTimer == null)
                {
                    _queueTimer = new Timer(
                        OnQueueTimerCallback,
                        null,
                        TimeSpan.FromMilliseconds(10000),
                        Timeout.InfiniteTimeSpan);
                }
                else
                {
                    _queueTimer.Change(TimeSpan.FromMilliseconds(10000), Timeout.InfiniteTimeSpan);
                }

                _queuedEvents.Add(new LibraryEvent { Item = item, EventType = eventType });
            }
        }

        /// <summary>
        /// Wait for timer callback to be completed.
        /// </summary>
        private async void OnQueueTimerCallback(object state)
        {
            try
            {
                await OnQueueTimerCallbackInternal().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnQueueTimerCallbackInternal");
            }
        }

        /// <summary>
        /// Wait for timer to be completed.
        /// </summary>
        private async Task OnQueueTimerCallbackInternal()
        {
            // _logger.LogInformation("Timer elapsed - processing queued items");
            List<LibraryEvent> queue;

            lock (_queuedEvents)
            {
                if (!_queuedEvents.Any())
                {
                    _logger.LogInformation("No events... stopping queue timer");
                    return;
                }

                queue = _queuedEvents.ToList();
                _queuedEvents.Clear();
            }

            var queuedMovieAdds = new List<LibraryEvent>();
            var queuedMovieUpdates = new List<LibraryEvent>();
            var queuedMovieForces = new List<LibraryEvent>();
            var queuedEpisodeAdds = new List<LibraryEvent>();
            var queuedEpisodeUpdates = new List<LibraryEvent>();
            var queuedEpisodeForces = new List<LibraryEvent>();
            var queuedShowAdds = new List<LibraryEvent>();
            var queuedShowUpdates = new List<LibraryEvent>();
            var queuedSeasonAdds = new List<LibraryEvent>();
            var queuedSeasonUpdates = new List<LibraryEvent>();

            // add事件可能会在获取元数据完之前执行，导致可能会中断元数据获取，通过pending集合把add事件延缓到获取元数据后再执行（获取完元数据后，一般会多推送一个update事件）
            foreach (var ev in queue)
            {
                // item所在的媒体库不启用弹幕插件，忽略处理
                if (IsIgnoreItem(ev.Item))
                {
                    continue;
                }

                if (ev.Item is Movie movie)
                {
                    if (ev.EventType == EventType.Add)
                    {
                        _logger.LogInformation("Movie add: {0}", movie.Name);
                        _memoryCache.Set<LibraryEvent>(movie.Id, ev, _pendingAddExpiredOption);
                    }
                    else if (ev.EventType == EventType.Update)
                    {
                        _logger.LogInformation("Movie update: {0}", movie.Name);
                        if (_memoryCache.TryGetValue<LibraryEvent>(movie.Id, out LibraryEvent addMovieEv))
                        {
                            queuedMovieAdds.Add(addMovieEv);
                            _memoryCache.Remove(movie.Id);
                        }
                        else
                        {
                            queuedMovieUpdates.Add(ev);
                        }
                    }
                    else if (ev.EventType == EventType.Force)
                    {
                        _logger.LogInformation("Movie force: {0}", movie.Name);
                        queuedMovieForces.Add(ev);
                    }
                }
                else if (ev.Item is Series series)
                {
                    if (ev.EventType == EventType.Add)
                    {
                        _logger.LogInformation("Series add: {0}", series.Name);
                        // 处理系列添加逻辑...
                    }
                    else if (ev.EventType == EventType.Update)
                    {
                        _logger.LogInformation("Series update: {0}", series.Name);
                        // 处理系列更新逻辑...
                    }
                }
                else if (ev.Item is Season season)
                {
                    var seasonId = season.GetSeasonId().ToString();
                    if (ev.EventType == EventType.Add)
                    {
                        _logger.LogInformation("Season add: {0}, id={1}", season.Name, seasonId);
                        _memoryCache.Set<LibraryEvent>(seasonId, ev, _pendingAddExpiredOption);
                    }
                    else if (ev.EventType == EventType.Update)
                    {
                        bool tryGetValue = _memoryCache.TryGetValue<LibraryEvent>(seasonId, out LibraryEvent addSeasonEv);
                        _logger.LogInformation("Season update: {0}, id={1}, tryGetValue={2}", season.Name, seasonId, tryGetValue);
                        if (tryGetValue)
                        {
                            queuedSeasonAdds.Add(addSeasonEv);
                            _memoryCache.Remove(seasonId);
                        }
                        else
                        {
                            queuedSeasonUpdates.Add(ev);
                        }
                    }
                }
                else if (ev.Item is Episode episode)
                {
                    if (ev.EventType == EventType.Update)
                    {
                        _logger.LogInformation("Episode update: {0}.{1}", episode.IndexNumber, episode.Name);
                        queuedEpisodeUpdates.Add(ev);
                    }
                    else if (ev.EventType == EventType.Force)
                    {
                        _logger.LogInformation("Episode force: {0}.{1}", episode.IndexNumber, episode.Name);
                        queuedEpisodeForces.Add(ev);
                    }
                }
            }

            // 对于剧集，处理顺序也很重要（Add事件后，会刷新元数据，导致会同时推送Update事件）
            await ProcessQueuedMovieEvents(queuedMovieAdds, EventType.Add).ConfigureAwait(false);
            await ProcessQueuedMovieEvents(queuedMovieUpdates, EventType.Update).ConfigureAwait(false);

            await ProcessQueuedShowEvents(queuedShowAdds, EventType.Add).ConfigureAwait(false);
            await ProcessQueuedSeasonEvents(queuedSeasonAdds, EventType.Add).ConfigureAwait(false);
            await ProcessQueuedEpisodeEvents(queuedEpisodeAdds, EventType.Add).ConfigureAwait(false);

            await ProcessQueuedShowEvents(queuedShowUpdates, EventType.Update).ConfigureAwait(false);
            await ProcessQueuedSeasonEvents(queuedSeasonUpdates, EventType.Update).ConfigureAwait(false);
            await ProcessQueuedEpisodeEvents(queuedEpisodeUpdates, EventType.Update).ConfigureAwait(false);

            await ProcessQueuedMovieEvents(queuedMovieForces, EventType.Force).ConfigureAwait(false);
            await ProcessQueuedEpisodeEvents(queuedEpisodeForces, EventType.Force).ConfigureAwait(false);
        }

        public bool IsIgnoreItem(BaseItem item)
        {
            // item所在的媒体库不启用弹幕插件，忽略处理
            var libraryOptions = _libraryManager.GetLibraryOptions(item);
            if (libraryOptions != null && libraryOptions.DisabledSubtitleFetchers.Contains(Plugin.Instance?.Name))
            {
                this._logger.LogInformation($"媒体库已关闭danmu插件, 忽略处理[{item.Name}].");
                return true;
            }

            return false;
        }


        /// <summary>
        /// Processes queued movie events.
        /// </summary>
        /// <param name="events">The <see cref="LibraryEvent"/> enumerable.</param>
        /// <param name="eventType">The <see cref="EventType"/>.</param>
        /// <returns>Task.</returns>
        public async Task ProcessQueuedMovieEvents(IReadOnlyCollection<LibraryEvent> events, EventType eventType)
        {
            if (events.Count == 0)
            {
                return;
            }

            var movies = new HashSet<Movie>(events
                .Select(lev => lev.Item as Movie) // 显式进行类型转换
                .Where(lev => lev != null && !string.IsNullOrEmpty(lev.Name))); // 确保movie 不是 null 之后再检查 Name 属性

            // 新增事件也会触发update，不需要处理Add
            // 更新，判断是否有bvid，有的话刷新弹幕文件
            if (eventType == EventType.Add)
            {
                // var queueUpdateMeta = new List<BaseItem>();
                foreach (var item in movies)
                {
                    foreach (var scraper in _scraperManager.All())
                    {
                        try
                        {
                            // 读取最新数据，要不然取不到年份信息
                            var currentItem = _libraryManager.GetItemById(item.InternalId) ?? item;
                            var mediaId = await scraper.SearchMediaId(currentItem);
                            if (string.IsNullOrEmpty(mediaId))
                            {
                                _logger.LogInformation("[{0}]匹配失败：{1} ({2})", scraper.Name, item.Name,
                                    item.ProductionYear);
                                continue;
                            }

                            var media = await scraper.GetMedia(item, mediaId);
                            if (media != null)
                            {
                                var providerVal = media.Id;
                                var commentId = media.CommentId;
                                _logger.LogInformation("[{0}]匹配成功：name={1} ProviderId: {2}, CommentId={3}", scraper.Name, item.Name,
                                    providerVal, media.CommentId);

                                // 更新epid元数据
                                item.SetProviderId(scraper.ProviderId, providerVal);
                                item.UpdateToRepository(ItemUpdateType.MetadataEdit);
                                // queueUpdateMeta.Add(item);

                                // 下载弹幕
                                await this.DownloadDanmu(scraper, item, commentId).ConfigureAwait(false);
                                if (!Config.OpenAllSource)
                                {
                                    break;
                                }
                            }
                        }
                        catch (FrequentlyRequestException ex)
                        {
                            _logger.LogError(ex, "[{0}]api接口触发风控，中止执行，请稍候再试.", scraper.Name);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "[{0}]Exception handled processing movie events", scraper.Name);
                        }
                    }
                }

                // await ProcessQueuedUpdateMeta(queueUpdateMeta).ConfigureAwait(false);
            }


            // 更新
            if (eventType == EventType.Update)
            {
                foreach (var item in movies)
                {
                    foreach (var scraper in _scraperManager.All())
                    {
                        try
                        {
                            var providerVal = item.GetProviderId(scraper.ProviderId);
                            if (!string.IsNullOrEmpty(providerVal))
                            {
                                var episode = await scraper.GetMediaEpisode(item, providerVal);
                                if (episode != null)
                                {
                                    // 下载弹幕xml文件
                                    await this.DownloadDanmu(scraper, item, episode.CommentId).ConfigureAwait(false);
                                }

                                // TODO：兼容支持用户设置seasonId？？？
                                break;
                            }
                        }
                        catch (FrequentlyRequestException ex)
                        {
                            _logger.LogError(ex, "api接口触发风控，中止执行，请稍候再试.");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Exception handled processing queued movie events");
                        }
                    }
                }
            }

            // 强制刷新指定来源弹幕
            if (eventType == EventType.Force)
            {
                foreach (var queueItem in movies)
                {
                    // 找到选择的scraper
                    var scraper = _scraperManager.All()
                        .FirstOrDefault(x => queueItem.ProviderIds.ContainsKey(x.ProviderId));
                    if (scraper == null)
                    {
                        continue;
                    }

                    // 获取选择的弹幕Id
                    var mediaId = queueItem.GetProviderId(scraper.ProviderId);
                    if (string.IsNullOrEmpty(mediaId))
                    {
                        continue;
                    }

                    // 获取最新的item数据
                    var item = _libraryManager.GetItemById(queueItem.Id);
                    var media = await scraper.GetMedia(item, mediaId);
                    if (media != null)
                    {
                        await this.ForceSaveProviderId(item, scraper.ProviderId, media.Id);

                        var episode = await scraper.GetMediaEpisode(item, media.Id);
                        if (episode != null)
                        {
                            // 下载弹幕xml文件
                            await this.DownloadDanmu(scraper, item, episode.CommentId, true).ConfigureAwait(false);
                        }
                    }
                }
            }
        }

        public async Task UpdateSeason(BaseItem item, bool force=false)
        {
            if (!force)
            {
                if (item.HasAnyDanmuProviderIds())
                {
                    return;
                }
            }

            EventType eventType = force ? EventType.Force : EventType.Add;
            List<LibraryEvent> libraryEvents = new List<LibraryEvent>() { new LibraryEvent(){Item= item, EventType = eventType} };
            if (item is Season)
            {
                await ProcessQueuedSeasonEvents(libraryEvents, eventType);
            }
            else if (item is Episode)
            {
                await ProcessQueuedEpisodeEvents(libraryEvents, eventType);
            }
        }

        /// <summary>
        /// Processes queued show events.
        /// </summary>
        /// <param name="events">The <see cref="LibraryEvent"/> enumerable.</param>
        /// <param name="eventType">The <see cref="EventType"/>.</param>
        /// <returns>Task.</returns>
        public async Task ProcessQueuedShowEvents(IReadOnlyCollection<LibraryEvent> events, EventType eventType)
        {
            if (events.Count == 0)
            {
                return;
            }

            _logger.LogDebug("Processing {Count} shows with event type {EventType}", events.Count, eventType);

            var series = new HashSet<Series>(events
                .Select(lev => lev.Item as Series) // 显式进行类型转换
                .Where(lev => lev != null && !string.IsNullOrEmpty(lev.Name)));

            try
            {
                if (eventType == EventType.Update)
                {
                    foreach (var item in series)
                    {
                        var seasons = item.GetSeasons(null, new DtoOptions(false));
                        foreach (var season in seasons)
                        {
                            // 发现season保存元数据，不会推送update事件，这里通过series的update事件推送刷新
                            QueueItem(season, eventType);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception handled processing queued show events");
            }
        }

        /// <summary>
        /// Processes queued season events.
        /// </summary>
        /// <param name="events">The <see cref="LibraryEvent"/> enumerable.</param>
        /// <param name="eventType">The <see cref="EventType"/>.</param>
        /// <returns>Task.</returns>
        public async Task ProcessQueuedSeasonEvents(IReadOnlyCollection<LibraryEvent> events, EventType eventType)
        {
            if (events.Count == 0)
            {
                return;
            }

            _logger.Info("Processing count={0} seasons with event type {1}", events.Count, eventType);
            var seasons = new HashSet<Season>(events
                .Select(lev => lev.Item as Season) // 显式进行类型转换
                .Where(lev => lev != null && !string.IsNullOrEmpty(lev.Name)));

            if (eventType == EventType.Add)
            {
                var queueUpdateMeta = new List<BaseItem>();
                foreach (var season in seasons)
                {
                    // // 虚拟季第一次请求忽略
                    // if (season.LocationType == LocationType.Virtual && season.IndexNumber is null)
                    // {
                    //     continue;
                    // }

                    if (season.IndexNumber.HasValue && season.IndexNumber == 0)
                    {
                        _logger.LogInformation("special特典文件夹不处理：name={0} number={1}", season.Name, season.IndexNumber);
                        continue;
                    }

                    var series = season.GetParent();
                    foreach (var scraper in _scraperManager.All())
                    {
                        try
                        {
                            // 读取最新数据，要不然取不到年份信息（不能对GetItemById的对象直接修改属性，要不然会直接改到数据！！！！）
                            var currentItem = _libraryManager.GetItemById(season.Id);
                            if (currentItem != null)
                            {
                                season.ProductionYear = currentItem.ProductionYear;
                            }

                            // 季的名称不准确，改使用series的名称
                            if (series != null)
                            {
                                season.Name = series.Name;
                            }

                            var mediaId = await scraper.SearchMediaId(season);
                            if (string.IsNullOrEmpty(mediaId))
                            {
                                _logger.LogInformation("[{0}]匹配失败：{1} ({2})", scraper.Name, season.Name,
                                    season.ProductionYear);
                                continue;
                            }

                            var media = await scraper.GetMedia(season, mediaId);
                            if (media == null)
                            {
                                _logger.LogInformation("[{0}]匹配成功，但获取不到视频信息. id: {1}", scraper.Name, mediaId);
                                continue;
                            }

                            // 更新seasonId元数据
                            season.SetProviderId(scraper.ProviderId, mediaId);
                            queueUpdateMeta.Add(season);

                            _logger.LogInformation("[{0}]匹配成功：name={1} season_number={2} ProviderId: {3}", scraper.Name,
                                season.Name, season.IndexNumber, mediaId);
                            if (!Config.OpenAllSource)
                            {
                                break;
                            }
                        }
                        catch (FrequentlyRequestException ex)
                        {
                            _logger.LogError(ex, "api接口触发风控，中止执行，请稍候再试.");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Exception handled processing season events");
                        }
                    }
                }

                // 保存元数据
                await ProcessQueuedUpdateMeta(queueUpdateMeta).ConfigureAwait(false);
            }

            if (eventType == EventType.Update)
            {
                foreach (var season in seasons)
                {
                    // // 虚拟季第一次请求忽略
                    // if (season.LocationType == LocationType.Virtual && season.IndexNumber is null)
                    // {
                    //     continue;
                    // }

                    var queueUpdateMeta = new List<BaseItem>();
                    // GetEpisodes一定要取所有fields，要不然更新会导致重建虚拟season季信息
                    // TODO：可能出现未刮削完，就触发获取弹幕，导致GetEpisodes只能获取到部分剧集的情况
                    var episodesItem = season.GetEpisodes();
                    if (episodesItem == null)
                    {
                        continue;
                    }

                    var episodes = episodesItem.Items.ToList();
                    var originEpisodes = episodes;

                    // 不处理季文件夹下的特典和extras影片（动画经常会混在一起）
                    var episodesWithoutSP = episodes.Where(x => x.ParentIndexNumber != null && x.ParentIndexNumber > 0)
                        .ToList();
                    if (episodes.Count != episodesWithoutSP.Count)
                    {
                        _logger.LogInformation("{0}季存在{1}个特典或extra片段，忽略处理.", season.Name,
                            (episodes.Count - episodesWithoutSP.Count));
                        episodes = episodesWithoutSP;
                    }

                    foreach (var scraper in _scraperManager.All())
                    {
                        try
                        {
                            var providerVal = season.GetDanmuProviderId(scraper.ProviderId);
                            if (string.IsNullOrEmpty(providerVal))
                            {
                                continue;   
                            }

                            var media = await scraper.GetMedia(season, providerVal);
                            if (media == null)
                            {
                                _logger.LogInformation("[{0}]获取不到视频信息. ProviderId: {1}", scraper.Name, providerVal);
                                break;
                            }

                            // 剧集可能更新中
                            int dabmuEpisodesCount = media.Episodes.Count;
                            if (ignoreEpisodesMatch && dabmuEpisodesCount != episodes.Count)
                            {
                                _logger.Info("[{0}]剧集数不匹配. 可能是更新中进行强制更新: {1}, media.Episodes={2}, episodes.Count={3}", scraper.Name, providerVal, dabmuEpisodesCount, episodes.Count);
                            }

                            if (episodes.Count != dabmuEpisodesCount && originEpisodes.Count == dabmuEpisodesCount)
                            {
                                _logger.LogInformation("{0}季忽略特典或extra片段后媒体数={1}, 未忽略媒体数={2} == 获取弹幕媒体数={3}, 现在使用未忽略媒体数据", season.Name, episodes.Count, originEpisodes.Count, dabmuEpisodesCount);
                                episodes = originEpisodes;
                            }
                            
                            int minEpisodes = Math.Min(episodes.Count, dabmuEpisodesCount);
                            _logger.LogInformation("[{0}]匹配完成，媒体数={1}. 弹幕数={2}, 最终需要匹配数={3}, 弹幕工具={4}", season.Name, episodes.Count(), dabmuEpisodesCount, minEpisodes, scraper.Name);
                            for (var idx = 0; idx < minEpisodes; idx++)
                            {
                                var episode = episodes[idx];
                                var fileName = Path.GetFileName(episode.Path);
                                var indexNumber = episode.IndexNumber ?? 0;
                                if (indexNumber <= 0)
                                {
                                    _logger.LogInformation("[{0}]匹配失败，缺少集号. [{1}]{2}", scraper.Name, season.Name, fileName);
                                    continue;
                                }

                                if (indexNumber > dabmuEpisodesCount)
                                {
                                    _logger.LogInformation("[{0}]匹配失败，集号超过总集数，可能识别集号错误. [{1}]{2} indexNumber: {3}",
                                        scraper.Name, season.Name, fileName, indexNumber);
                                    continue;
                                }

                                if (ignoreEpisodesMatch || dabmuEpisodesCount == episodes.Count)
                                {
                                    var epId = media.Episodes[idx].Id;
                                    var commentId = media.Episodes[idx].CommentId;
                                    _logger.LogInformation("[{0}]成功匹配. {1}.{2} -> epId: {3} cid: {4}", scraper.Name,
                                        indexNumber, episode.Name, epId, commentId);

                                    // 更新eposide元数据
                                    var episodeProviderVal = episode.GetProviderId(scraper.ProviderId);
                                    if (!string.IsNullOrEmpty(epId) && episodeProviderVal != epId)
                                    {
                                        episode.SetProviderId(scraper.ProviderId, epId);
                                        queueUpdateMeta.Add(episode);
                                    }

                                    var danmuXmlPath = Path.Combine(episode.ContainingFolderPath, episode.GetDanmuXmlPath(scraper.ProviderId));
                                    var lastWriteTime = this._fileSystem.GetLastWriteTime(danmuXmlPath);
                                    var diff = DateTime.Now - lastWriteTime;
                                    if (diff.TotalSeconds < 3600)
                                    {
                                        // 
                                        _logger.Info("{0}弹幕文件在1小时内更新过, 忽略， 弹幕文件={1}", episode.Name, danmuXmlPath);
                                        continue;
                                    }

                                    // 下载弹幕
                                    await this.DownloadDanmu(scraper, episode, commentId).ConfigureAwait(false);
                                }
                                else
                                {
                                    _logger.LogInformation("[{0}]刷新弹幕失败, 集数不一致。video: {1}.{2} 弹幕数：{3} 集数：{4}",
                                        scraper.Name, indexNumber, episode.Name, dabmuEpisodesCount, episodes.Count);
                                }
                            }

                            if (!Config.OpenAllSource)
                            {
                                break;
                            }
                        }
                        catch (FrequentlyRequestException ex)
                        {
                            _logger.LogError(ex, "api接口触发风控，中止执行，请稍候再试.");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Exception handled processing queued movie events");
                        }
                    }

                    // 保存元数据
                    await ProcessQueuedUpdateMeta(queueUpdateMeta).ConfigureAwait(false);
                }
            }
        }


        /// <summary>
        /// Processes queued episode events.
        /// </summary>
        /// <param name="events">The <see cref="LibraryEvent"/> enumerable.</param>
        /// <param name="eventType">The <see cref="EventType"/>.</param>
        /// <returns>Task.</returns>
        public async Task ProcessQueuedEpisodeEvents(IReadOnlyCollection<LibraryEvent> events, EventType eventType)
        {
            if (events.Count == 0)
            {
                return;
            }

            _logger.LogDebug("Processing {Count} episodes with event type {EventType}", events.Count, eventType);

            var episodes = new HashSet<Episode>(events
                .Select(lev => lev.Item as Episode) // 显式进行类型转换
                .Where(lev => lev != null && !string.IsNullOrEmpty(lev.Name))); // 确保movie 不是 null 之后再检查 Name 属性

            // 判断epid，有的话刷新弹幕文件
            if (eventType == EventType.Update)
            {
                foreach (var item in episodes)
                {
                    foreach (var scraper in _scraperManager.All())
                    {
                        try
                        {
                            var providerVal = item.GetProviderId(scraper.ProviderId);
                            if (string.IsNullOrEmpty(providerVal))
                            {
                                providerVal = await GetEpisodeDanmuIdBySeason(item.Season, item, scraper).ConfigureAwait(false);
                                if (string.IsNullOrEmpty(providerVal))
                                {
                                    continue;
                                }
                            }

                            var episode = await scraper.GetMediaEpisode(item, providerVal);
                            if (episode != null)
                            {
                                // 下载弹幕xml文件
                                await this.DownloadDanmu(scraper, item, episode.CommentId).ConfigureAwait(false);
                            }
                        }
                        catch (FrequentlyRequestException ex)
                        {
                            _logger.LogError(ex, "api接口触发风控，中止执行，请稍候再试.");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Exception handled processing queued movie events");
                        }
                    }
                }
            }


            // 强制刷新指定来源弹幕（手动搜索强刷忽略集数不一致处理）
            if (eventType == EventType.Force)
            {
                foreach (var queueItem in episodes)
                {
                    // 找到选择的scraper
                    var scraper = _scraperManager.All()
                        .FirstOrDefault(x => queueItem.ProviderIds.ContainsKey(x.ProviderId));
                    if (scraper == null)
                    {
                        continue;
                    }

                    // 获取选择的弹幕Id
                    var mediaId = queueItem.GetProviderId(scraper.ProviderId);
                    if (string.IsNullOrEmpty(mediaId))
                    {
                        continue;
                    }

                    // 获取最新的item数据
                    var item = _libraryManager.GetItemById(queueItem.Id);
                    var season = ((Episode)item).Season;
                    if (season == null)
                    {
                        continue;
                    }

                    var media = await scraper.GetMedia(season, mediaId);
                    if (media != null)
                    {
                        // 更新季元数据
                        await ForceSaveProviderId(season, scraper.ProviderId, media.Id);

                        // 更新所有剧集元数据，GetEpisodes一定要取所有fields，要不然更新会导致重建虚拟season季信息
                        var episodeItemResult = season.GetEpisodes();
                        var episodeList = episodeItemResult.Items;
                        for (var idx = 0; idx < episodeList.Count(); idx++)
                        {
                            var episode = episodeList[idx];
                            var fileName = Path.GetFileName(episode.Path);

                            // 没对应剧集号的，忽略处理
                            var indexNumber = episode.IndexNumber ?? 0;
                            if (indexNumber < 1 || indexNumber > media.Episodes.Count)
                            {
                                _logger.LogInformation("[{0}]缺少集号或集号超过弹幕数，忽略处理. [{1}]{2}, indexNumber={3}, mediaCount={4}", scraper.Name, season.Name, fileName, indexNumber, media.Episodes.Count);
                                continue;
                            }

                            // 特典或extras影片不处理（动画经常会放在季文件夹下）
                            if (episode.ParentIndexNumber == null || episode.ParentIndexNumber == 0)
                            {
                                _logger.LogInformation("[{0}]缺少季号，可能是特典或extras影片，忽略处理. [{1}]{2}", scraper.Name,
                                    season.Name, fileName);
                                continue;
                            }

                            var epId = media.Episodes[indexNumber - 1].Id;
                            var commentId = media.Episodes[indexNumber - 1].CommentId;

                            // 下载弹幕xml文件
                            await this.DownloadDanmu(scraper, episode, commentId, true).ConfigureAwait(false);

                            // 更新剧集元数据
                            await ForceSaveProviderId(episode, scraper.ProviderId, epId);
                        }
                    }
                }
            }
        }


        // 调用UpdateToRepositoryAsync后，但未完成时，会导致GetEpisodes返回缺少正在处理的集数，所以采用统一最后处理
        private Task ProcessQueuedUpdateMeta(List<BaseItem> queue)
        {
            if (queue == null || queue.Count <= 0)
            {
                return Task.CompletedTask;
            }

            foreach (var queueItem in queue)
            {
                // 获取最新的item数据
                var queueItemId = queueItem.Id;
                if (Guid.Empty.Equals(queueItemId) && queueItem is Season)
                {
                    queueItemId = queueItem.GetParent().Id;
                    _logger.LogInformation("当前是Season={0}, 并且不存在相应的id，使用Series信息={1}", queueItem.Name, queueItemId);
                }
                
                var item = _libraryManager.GetItemById(queueItemId);
                if (item != null)
                {
                    // 合并新添加的provider id
                    foreach (var pair in queueItem.ProviderIds)
                    {
                        if (string.IsNullOrEmpty(pair.Value))
                        {
                            continue;
                        }

                        item.ProviderIds[pair.Key] = pair.Value;
                    }

                    item.UpdateToRepository(ItemUpdateType.MetadataEdit);
                    // Console.WriteLine(JsonSerializer.Serialize(item));
                    // await item.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, CancellationToken.None).ConfigureAwait(false);
                }
            }

            _logger.LogInformation("更新epid到元数据完成。item数：{0}", queue.Count);
            return Task.CompletedTask;
        }

        public async Task DownloadDanmu(AbstractScraper scraper, BaseItem item, string commentId,
            bool ignoreCheck = false)
        {
            // 下载弹幕xml文件
            var checkDownloadedKey = $"{item.Id}_{commentId}";
            try
            {
                // 弹幕5分钟内更新过，忽略处理（有时Update事件会重复执行）
                if (!SingletonManager.IsDebug && !ignoreCheck && _memoryCache.TryGetValue(checkDownloadedKey, out var latestDownloaded))
                {
                    _logger.LogInformation("[{0}]最近5分钟已更新过弹幕xml，忽略处理：{1}.{2}", scraper.Name, item.IndexNumber,
                        item.Name);
                    return;
                }

                _memoryCache.Set(checkDownloadedKey, true, _danmuUpdatedExpiredOption);
                var danmaku = await scraper.GetDanmuContent(item, commentId);
                if (danmaku != null)
                {
                    var bytes = danmaku.ToXml();
                    if (bytes.Length < 1024)
                    {
                        _logger.LogInformation("[{0}]弹幕内容少于1KB，忽略处理：{1}.{2}", scraper.Name, item.IndexNumber,
                            item.Name);
                        return;
                    }

                    await this.SaveDanmu(scraper, item, bytes);
                    this._logger.LogInformation("[{0}]弹幕下载成功：name={1}.{2} commentId={3}", scraper.Name,
                        item.IndexNumber ?? 1, item.Name, commentId);
                }
                else
                {
                    _memoryCache.Remove(checkDownloadedKey);
                }
            }
            catch (Exception ex)
            {
                _memoryCache.Remove(checkDownloadedKey);
                _logger.LogError(ex, "[{0}]Exception handled download danmu file. name={1}", scraper.Name, item.Name);
            }
        }

        private bool IsRepeatAction(BaseItem item, string checkDownloadedKey)
        {
            // 单元测试时为null
            if (item.FileNameWithoutExtension == null) return false;

            // 通过xml文件属性判断（多线程时判断有误）
            var danmuPath = Path.Combine(item.ContainingFolderPath, item.FileNameWithoutExtension + ".xml");
            if (!this._fileSystem.Exists(danmuPath))
            {
                return false;
            }

            var lastWriteTime = this._fileSystem.GetLastWriteTime(danmuPath);
            var diff = DateTime.Now - lastWriteTime;
            return diff.TotalSeconds < 300;
        }

        private async Task SaveDanmu(AbstractScraper scraper, BaseItem item, byte[] bytes)
        {
            // 单元测试时为null
            if (item.FileNameWithoutExtension == null) return;

            // 下载弹幕xml文件
            var danmuPath = Path.Combine(item.ContainingFolderPath, item.GetDanmuXmlPath(scraper.ProviderId));
            try
            {
                await this._fileSystem.WriteAllBytesAsync(danmuPath, bytes, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.ErrorException("文件写入异常 danmuPath={0}", e, danmuPath);
                throw;
            }

            if (this.Config.ToAss && bytes.Length > 0)
            {
                var assConfig = new Danmaku2Ass.Config();
                assConfig.Title = item.Name;
                if (!string.IsNullOrEmpty(this.Config.AssFont.Trim()))
                {
                    assConfig.FontName = this.Config.AssFont;
                }

                if (!string.IsNullOrEmpty(this.Config.AssFontSize.Trim()))
                {
                    assConfig.BaseFontSize = this.Config.AssFontSize.Trim().ToInt();
                }

                if (!string.IsNullOrEmpty(this.Config.AssTextOpacity.Trim()))
                {
                    assConfig.TextOpacity = this.Config.AssTextOpacity.Trim().ToFloat();
                }

                if (!string.IsNullOrEmpty(this.Config.AssLineCount.Trim()))
                {
                    assConfig.LineCount = this.Config.AssLineCount.Trim().ToInt();
                }

                if (!string.IsNullOrEmpty(this.Config.AssSpeed.Trim()))
                {
                    assConfig.TuneDuration = this.Config.AssSpeed.Trim().ToInt() - 8;
                }

                var assPath = Path.Combine(item.ContainingFolderPath, item.FileNameWithoutExtension + ".chs[" + scraper.ProviderId + "_danmu].ass");
                Danmaku2Ass.Bilibili.GetInstance().Create(bytes, assConfig, assPath);
            }
        }

        private async Task ForceSaveProviderId(BaseItem item, string providerId, string providerVal)
        {
            _logger.Info("ForceSaveProviderId item={0}, providerId={1}, providerVal={2}", item?.GetParent(), providerId, providerVal);
            var updateItem = item;
            // Season 不存在需要更新到 Series上
            if (Guid.Empty.Equals(updateItem.Id) && updateItem is Season)
            {
                updateItem = item.GetParent();
            }

            // 先清空旧弹幕的所有元数据
            foreach (var s in _scraperManager.All())
            {
                updateItem.ProviderIds.Remove(s.ProviderId);
            }

            // 保存指定弹幕元数据
            updateItem.ProviderIds[providerId] = providerVal;
            _logger.Info("1");
            await updateItem.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, CancellationToken.None)
                .ConfigureAwait(false);
            _logger.Info("2");
        }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _queueTimer?.Dispose();
            }
        }

        public async Task<string> GetEpisodeDanmuIdBySeason(Season season, Episode episode, AbstractScraper scraper)
        {
            if (season == null || episode == null)
            {
                return null;
            }
            
            var providerVal = season.GetDanmuProviderId(scraper.ProviderId);
            if (string.IsNullOrEmpty(providerVal))
            {
                return providerVal;
            }

            var episodesItem = season.GetEpisodes();
            if (episodesItem == null)
            {
                return null;
            }
            var episodes = episodesItem.Items.ToList();
            if (episodes.Count == 0)
            {
                return null;
            }
            
            string cacheKey = $"{season.GetSeasonId().ToString()}_{scraper.ProviderId}";
            if (!_memoryCache.TryGetValue(cacheKey, out ScraperMedia media))
            {
                media = await scraper.GetMedia(season, providerVal);
                _memoryCache.Set(cacheKey, media);
                if (media == null)
                {
                    _logger.LogInformation("[{0}]获取不到视频信息. ProviderId: {1}", scraper.Name, providerVal);
                    return null;
                }
            }

            // 剧集可能更新中
            if (ignoreEpisodesMatch && media.Episodes.Count != episodes.Count)
            {
                _logger.Info("[{0}]剧集数不匹配. 可能是更新中进行强制更新: {1}, media.Episodes={2}, episodes.Count={3}", scraper.Name, providerVal, media.Episodes.Count, episodes.Count);
            }

            // 获取
            var fileName = Path.GetFileName(episode.Path);
            int episodeIndexNumber = episode.IndexNumber ?? 0;
            if (episodeIndexNumber < 1 || episodeIndexNumber>media.Episodes.Count)
            {
                _logger.LogInformation("[{0}]缺少集号或集号超过弹幕数，忽略处理. [{1}]{2}, indexNumber={3}, mediaCount={4}", scraper.Name, season.Name, fileName, episodeIndexNumber, media.Episodes.Count);
                return null;
            }
            
            // 特典或extras影片不处理（动画经常会放在季文件夹下）
            if (episode.ParentIndexNumber == null || episode.ParentIndexNumber == 0)
            {
                _logger.LogInformation("[{0}]缺少季号，可能是特典或extras影片，忽略处理. [{1}]{2}", scraper.Name,
                    season.Name, fileName);
                return null;
            }
            
            var epId = media.Episodes[episodeIndexNumber - 1].Id;
            // 更新剧集元数据
            await ForceSaveProviderId(episode, scraper.ProviderId, epId);
            return epId;
        }
    }
}