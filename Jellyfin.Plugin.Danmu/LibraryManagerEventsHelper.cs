using System.Runtime.InteropServices;
using System.Net.Http;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Danmu.Core;
using Jellyfin.Plugin.Danmu.Model;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Controller.Persistence;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using Jellyfin.Plugin.Danmu.Scrapers;
using Jellyfin.Plugin.Danmu.Core.Extensions;
using Jellyfin.Plugin.Danmu.Configuration;

namespace Jellyfin.Plugin.Danmu;

public class LibraryManagerEventsHelper : IDisposable
{
    private readonly List<LibraryEvent> _queuedEvents;
    private readonly IMemoryCache _pendingAddEventCache;
    private readonly MemoryCacheEntryOptions _expiredOption = new MemoryCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) };

    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<LibraryManagerEventsHelper> _logger;
    private readonly Jellyfin.Plugin.Danmu.Core.IFileSystem _fileSystem;
    private Timer _queueTimer;
    private readonly ScraperManager _scraperManager;

    public PluginConfiguration Config
    {
        get
        {
            return Plugin.Instance?.Configuration ?? new Configuration.PluginConfiguration();
        }
    }


    /// <summary>
    /// Initializes a new instance of the <see cref="LibraryManagerEventsHelper"/> class.
    /// </summary>
    /// <param name="libraryManager">The <see cref="ILibraryManager"/>.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    /// <param name="api">The <see cref="BilibiliApi"/>.</param>
    /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
    public LibraryManagerEventsHelper(ILibraryManager libraryManager, ILoggerFactory loggerFactory, Jellyfin.Plugin.Danmu.Core.IFileSystem fileSystem, ScraperManager scraperManager)
    {
        _queuedEvents = new List<LibraryEvent>();
        _pendingAddEventCache = new MemoryCache(new MemoryCacheOptions());

        _libraryManager = libraryManager;
        _logger = loggerFactory.CreateLogger<LibraryManagerEventsHelper>();
        _fileSystem = fileSystem;
        _scraperManager = scraperManager;
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
        var queuedEpisodeAdds = new List<LibraryEvent>();
        var queuedEpisodeUpdates = new List<LibraryEvent>(); ;
        var queuedShowAdds = new List<LibraryEvent>();
        var queuedShowUpdates = new List<LibraryEvent>();
        var queuedSeasonAdds = new List<LibraryEvent>();
        var queuedSeasonUpdates = new List<LibraryEvent>();

        // add????????????????????????????????????????????????????????????????????????????????????????????????pending?????????add?????????????????????????????????????????????????????????????????????????????????????????????update?????????
        foreach (var ev in queue)
        {
            switch (ev.Item)
            {
                case Movie when ev.EventType is EventType.Add:
                    _logger.LogInformation("Movie add: {0}", ev.Item.Name);
                    _pendingAddEventCache.Set<LibraryEvent>(ev.Item.Id, ev, _expiredOption);
                    break;
                case Movie when ev.EventType is EventType.Update:
                    _logger.LogInformation("Movie update: {0}", ev.Item.Name);
                    if (_pendingAddEventCache.TryGetValue<LibraryEvent>(ev.Item.Id, out LibraryEvent addMovieEv))
                    {
                        queuedMovieAdds.Add(addMovieEv);
                        _pendingAddEventCache.Remove(ev.Item.Id);
                    }
                    else
                    {
                        queuedMovieUpdates.Add(ev);
                    }
                    break;
                case Series when ev.EventType is EventType.Add:
                    _logger.LogInformation("Series add: {0}", ev.Item.Name);
                    _pendingAddEventCache.Set<LibraryEvent>(ev.Item.Id, ev, _expiredOption);
                    break;
                case Series when ev.EventType is EventType.Update:
                    _logger.LogInformation("Series update: {0}", ev.Item.Name);
                    if (_pendingAddEventCache.TryGetValue<LibraryEvent>(ev.Item.Id, out LibraryEvent addSerieEv))
                    {
                        // ??????add?????????update?????????????????????
                        _pendingAddEventCache.Remove(ev.Item.Id);
                    }
                    else
                    {
                        queuedShowUpdates.Add(ev);
                    }
                    break;
                case Season when ev.EventType is EventType.Add:
                    _logger.LogInformation("Season add: {0}", ev.Item.Name);
                    _pendingAddEventCache.Set<LibraryEvent>(ev.Item.Id, ev, _expiredOption);
                    break;
                case Season when ev.EventType is EventType.Update:
                    _logger.LogInformation("Season update: {0}", ev.Item.Name);
                    if (_pendingAddEventCache.TryGetValue<LibraryEvent>(ev.Item.Id, out LibraryEvent addSeasonEv))
                    {
                        queuedSeasonAdds.Add(addSeasonEv);
                        _pendingAddEventCache.Remove(ev.Item.Id);
                    }
                    else
                    {
                        queuedSeasonUpdates.Add(ev);
                    }
                    break;
                case Episode when ev.EventType is EventType.Update:
                    _logger.LogInformation("Episode update: {0}", ev.Item.Name);
                    queuedEpisodeUpdates.Add(ev);
                    break;
            }

        }

        // ??????????????????????????????????????????Add??????????????????????????????????????????????????????Update?????????
        await ProcessQueuedMovieEvents(queuedMovieAdds, EventType.Add).ConfigureAwait(false);
        await ProcessQueuedMovieEvents(queuedMovieUpdates, EventType.Update).ConfigureAwait(false);

        await ProcessQueuedShowEvents(queuedShowAdds, EventType.Add).ConfigureAwait(false);
        await ProcessQueuedSeasonEvents(queuedSeasonAdds, EventType.Add).ConfigureAwait(false);
        await ProcessQueuedEpisodeEvents(queuedEpisodeAdds, EventType.Add).ConfigureAwait(false);

        await ProcessQueuedShowEvents(queuedShowUpdates, EventType.Update).ConfigureAwait(false);
        await ProcessQueuedSeasonEvents(queuedSeasonUpdates, EventType.Update).ConfigureAwait(false);
        await ProcessQueuedEpisodeEvents(queuedEpisodeUpdates, EventType.Update).ConfigureAwait(false);
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

        _logger.LogDebug("Processing {Count} movies with event type {EventType}", events.Count, eventType);

        var movies = events.Select(lev => (Movie)lev.Item)
            .Where(lev => !string.IsNullOrEmpty(lev.Name))
            .ToHashSet();


        // ????????????????????????update??????????????????Add
        // ????????????????????????bvid??????????????????????????????
        if (eventType == EventType.Add)
        {
            var queueUpdateMeta = new List<BaseItem>();
            foreach (var item in movies)
            {
                foreach (var scraper in _scraperManager.All())
                {
                    try
                    {
                        // ???????????????????????????????????????????????????
                        var currentItem = _libraryManager.GetItemById(item.Id) ?? item;

                        var mediaId = await scraper.GetMatchMediaId(currentItem);
                        if (string.IsNullOrEmpty(mediaId))
                        {
                            _logger.LogInformation("[{0}]???????????????{1}", scraper.Name, item.Name);
                            continue;
                        }

                        var media = await scraper.GetMedia(mediaId);
                        if (media != null && media.Episodes.Count > 0)
                        {
                            var providerVal = media.Episodes[0].Id;
                            var commentId = media.Episodes[0].CommentId;
                            _logger.LogInformation("[{0}]???????????????name={1} ProviderId: {2}", scraper.Name, item.Name, providerVal);

                            // ??????epid?????????
                            item.SetProviderId(scraper.ProviderId, providerVal);
                            queueUpdateMeta.Add(item);

                            // ????????????
                            await this.DownloadDanmu(scraper, item, commentId).ConfigureAwait(false);
                            break;
                        }
                    }
                    catch (FrequentlyRequestException ex)
                    {
                        _logger.LogError(ex, "[{0}]api???????????????????????????????????????????????????.", scraper.Name);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[{0}]Exception handled processing queued movie events", scraper.Name);
                    }
                }
            }

            await ProcessQueuedUpdateMeta(queueUpdateMeta).ConfigureAwait(false);
        }


        // ??????
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
                            var episode = await scraper.GetMediaEpisode(providerVal);
                            if (episode != null)
                            {
                                // ????????????xml??????
                                await this.DownloadDanmu(scraper, item, episode.CommentId).ConfigureAwait(false);
                            }

                            // TODO???????????????????????????seasonId?????????
                            break;
                        }
                    }
                    catch (FrequentlyRequestException ex)
                    {
                        _logger.LogError(ex, "api???????????????????????????????????????????????????.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Exception handled processing queued movie events");
                    }
                }
            }
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

        var series = events.Select(lev => (Series)lev.Item)
            .Where(lev => !string.IsNullOrEmpty(lev.Name))
            .ToHashSet();

        try
        {
            if (eventType == EventType.Update)
            {
                foreach (var item in series)
                {
                    var seasons = item.GetSeasons(null, new DtoOptions(false));
                    foreach (var season in seasons)
                    {
                        // ??????season??????????????????????????????update?????????????????????series???update??????????????????
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

        _logger.LogDebug("Processing {Count} seasons with event type {EventType}", events.Count, eventType);

        var seasons = events.Select(lev => (Season)lev.Item)
            .Where(lev => !string.IsNullOrEmpty(lev.Name))
            .ToHashSet();


        if (eventType == EventType.Add)
        {
            var queueUpdateMeta = new List<BaseItem>();
            foreach (var season in seasons)
            {
                if (season.IndexNumber.HasValue && season.IndexNumber == 0)
                {
                    _logger.LogInformation("??????????????????name={0} number={1}", season.Name, season.IndexNumber);
                    continue;
                }

                var series = season.GetParent();
                foreach (var scraper in _scraperManager.All())
                {
                    try
                    {
                        // ???????????????????????????????????????????????????
                        var currentItem = _libraryManager.GetItemById(season.Id) ?? season;
                        // ?????????????????????????????????series?????????
                        if (series != null)
                        {
                            currentItem.Name = series.Name;
                        }
                        var mediaId = await scraper.GetMatchMediaId(currentItem);

                        if (!string.IsNullOrEmpty(mediaId))
                        {
                            // ??????seasonId?????????
                            season.SetProviderId(scraper.ProviderId, mediaId);
                            queueUpdateMeta.Add(season);

                            _logger.LogInformation("[{0}]???????????????name={1} season_number={2} ProviderId: {3}", scraper.Name, season.Name, season.IndexNumber, mediaId);
                            break;
                        }
                    }
                    catch (FrequentlyRequestException ex)
                    {
                        _logger.LogError(ex, "api???????????????????????????????????????????????????.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Exception handled processing queued movie events");
                    }
                }
            }

            // ???????????????
            await ProcessQueuedUpdateMeta(queueUpdateMeta).ConfigureAwait(false);
        }

        if (eventType == EventType.Update)
        {
            foreach (var season in seasons)
            {
                var queueUpdateMeta = new List<BaseItem>();
                var episodes = season.GetEpisodes(null, new DtoOptions(false));
                if (episodes == null)
                {
                    continue;
                }

                foreach (var scraper in _scraperManager.All())
                {
                    try
                    {
                        var providerVal = season.GetProviderId(scraper.ProviderId);
                        if (string.IsNullOrEmpty(providerVal))
                        {
                            continue;
                        }

                        var media = await scraper.GetMedia(providerVal);
                        if (media == null)
                        {
                            _logger.LogInformation("[{0}]????????????????????????. ProviderId: {1}", scraper.Name, providerVal);
                            break;
                        }

                        foreach (var (episode, idx) in episodes.WithIndex())
                        {
                            var indexNumber = episode.IndexNumber ?? 0;
                            if (indexNumber <= 0)
                            {
                                _logger.LogInformation("[{0}]???????????????????????????. [{1}]{2}", scraper.Name, season.Name, episode.Name);
                                continue;
                            }

                            if (indexNumber > media.Episodes.Count)
                            {
                                _logger.LogInformation("[{0}]?????????????????????????????????????????????????????????. [{1}]{2} indexNumber: {3}", scraper.Name, season.Name, episode.Name, indexNumber);
                                continue;
                            }

                            if (media.Episodes.Count == episodes.Count)
                            {
                                var epId = media.Episodes[idx].Id;
                                var commentId = media.Episodes[idx].CommentId;
                                _logger.LogInformation("[{0}]????????????. {1} -> epId: {2} cid: {3}", scraper.Name, episode.Name, epId, commentId);

                                // ??????eposide?????????
                                var episodeProviderVal = episode.GetProviderId(scraper.ProviderId);
                                if (!string.IsNullOrEmpty(epId) && episodeProviderVal != epId)
                                {
                                    episode.SetProviderId(scraper.ProviderId, epId);
                                    queueUpdateMeta.Add(episode);
                                }

                                // ????????????
                                await this.DownloadDanmu(scraper, episode, commentId).ConfigureAwait(false);
                            }
                            else
                            {
                                _logger.LogInformation("[{0}]??????????????????, ??????????????????video: {1} ????????????{2} ?????????{3}", scraper.Name, season.Name, media.Episodes.Count, episodes.Count);
                            }
                        }

                        break;

                    }
                    catch (FrequentlyRequestException ex)
                    {
                        _logger.LogError(ex, "api???????????????????????????????????????????????????.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Exception handled processing queued movie events");
                    }
                }

                // ???????????????
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

        var episodes = events.Select(lev => (Episode)lev.Item)
            .Where(lev => !string.IsNullOrEmpty(lev.Name))
            .ToHashSet();


        // ??????epid??????????????????????????????
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
                            continue;
                        }

                        var episode = await scraper.GetMediaEpisode(providerVal);
                        if (episode != null)
                        {
                            // ????????????xml??????
                            await this.DownloadDanmu(scraper, item, episode.CommentId).ConfigureAwait(false);
                        }
                        break;
                    }
                    catch (FrequentlyRequestException ex)
                    {
                        _logger.LogError(ex, "api???????????????????????????????????????????????????.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Exception handled processing queued movie events");
                    }
                }
            }

        }
    }


    // ??????UpdateToRepositoryAsync?????????????????????????????????GetEpisodes??????????????????????????????????????????????????????????????????
    private async Task ProcessQueuedUpdateMeta(List<BaseItem> queue)
    {
        if (queue == null || queue.Count <= 0)
        {
            return;
        }

        foreach (var queueItem in queue)
        {
            // ???????????????item??????
            var item = _libraryManager.GetItemById(queueItem.Id);
            if (item != null)
            {
                // ??????????????????provider id
                foreach (var pair in queueItem.ProviderIds)
                {
                    item.ProviderIds[pair.Key] = pair.Value;
                }

                // Console.WriteLine(JsonSerializer.Serialize(item));
                await item.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, CancellationToken.None).ConfigureAwait(false);
            }
        }
        _logger.LogInformation("??????epid?????????????????????item??????{0}", queue.Count);
    }

    public async Task DownloadDanmu(AbstractScraper scraper, BaseItem item, string commentId)
    {
        // ????????????xml??????
        try
        {
            // ???????????????????????????????????????????????????Update????????????????????????
            if (IsRepeatAction(item))
            {
                _logger.LogInformation("[{0}]??????1????????????????????????xml??????????????????{1}", scraper.Name, item.Name);
                return;
            }

            var danmaku = await scraper.GetDanmuContent(commentId);
            if (danmaku != null)
            {
                await this.DownloadDanmuInternal(item, danmaku.ToXml());
                this._logger.LogInformation("[{0}]?????????????????????name={1}", scraper.Name, item.Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{0}]Exception handled download danmu file. name={1}", scraper.Name, item.Name);
        }
    }

    private bool IsRepeatAction(BaseItem item)
    {
        var danmuPath = Path.Combine(item.ContainingFolderPath, item.FileNameWithoutExtension + ".xml");
        if (!this._fileSystem.Exists(danmuPath))
        {
            return false;
        }

        var lastWriteTime = this._fileSystem.GetLastWriteTime(danmuPath);
        var diff = DateTime.Now - lastWriteTime;
        return diff.TotalSeconds < 60;
    }

    private async Task DownloadDanmuInternal(BaseItem item, byte[] bytes)
    {
        // ????????????xml??????
        var danmuPath = Path.Combine(item.ContainingFolderPath, item.FileNameWithoutExtension + ".xml");
        await this._fileSystem.WriteAllBytesAsync(danmuPath, bytes, CancellationToken.None).ConfigureAwait(false);

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

            var assPath = Path.Combine(item.ContainingFolderPath, item.FileNameWithoutExtension + ".danmu.ass");
            Danmaku2Ass.Bilibili.GetInstance().Create(bytes, assConfig, assPath);
        }
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
}
