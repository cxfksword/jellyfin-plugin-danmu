using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Danmaku2Ass;
using Jellyfin.Plugin.Danmu.Api;
using Jellyfin.Plugin.Danmu.Api.Entity;
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
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Danmu.Providers;

public class LibraryManagerEventsHelper : IDisposable
{
    private readonly List<LibraryEvent> _queuedEvents;

    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<LibraryManagerEventsHelper> _logger;
    private readonly BilibiliApi _api;
    private readonly IFileSystem _fileSystem;
    private Timer _queueTimer;


    /// <summary>
    /// Initializes a new instance of the <see cref="LibraryManagerEventsHelper"/> class.
    /// </summary>
    /// <param name="libraryManager">The <see cref="ILibraryManager"/>.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    /// <param name="api">The <see cref="BilibiliApi"/>.</param>
    /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
    public LibraryManagerEventsHelper(ILibraryManager libraryManager, ILoggerFactory loggerFactory, BilibiliApi api, IFileSystem fileSystem)
    {
        _queuedEvents = new List<LibraryEvent>();

        _libraryManager = libraryManager;
        _logger = loggerFactory.CreateLogger<LibraryManagerEventsHelper>();
        _api = api;
        _fileSystem = fileSystem;
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
        _logger.LogInformation("Timer elapsed - processing queued items");
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

        var queuedMovieDeletes = new List<LibraryEvent>();
        var queuedMovieAdds = new List<LibraryEvent>();
        var queuedMovieUpdates = new List<LibraryEvent>();
        var queuedMovieRefreshs = new List<LibraryEvent>();
        var queuedEpisodeDeletes = new List<LibraryEvent>();
        var queuedEpisodeAdds = new List<LibraryEvent>();
        var queuedEpisodeUpdates = new List<LibraryEvent>();
        var queuedEpisodeRefreshs = new List<LibraryEvent>();
        var queuedShowDeletes = new List<LibraryEvent>();
        var queuedShowAdds = new List<LibraryEvent>();
        var queuedShowUpdates = new List<LibraryEvent>();
        var queuedShowRefreshs = new List<LibraryEvent>();
        var queuedSeasonDeletes = new List<LibraryEvent>();
        var queuedSeasonAdds = new List<LibraryEvent>();
        var queuedSeasonUpdates = new List<LibraryEvent>();
        var queuedSeasonRefreshs = new List<LibraryEvent>();


        queuedMovieDeletes.Clear();
        queuedMovieAdds.Clear();
        queuedMovieUpdates.Clear();
        queuedMovieRefreshs.Clear();
        queuedEpisodeDeletes.Clear();
        queuedEpisodeAdds.Clear();
        queuedEpisodeUpdates.Clear();
        queuedEpisodeRefreshs.Clear();
        queuedShowDeletes.Clear();
        queuedShowAdds.Clear();
        queuedShowUpdates.Clear();
        queuedShowRefreshs.Clear();
        queuedSeasonDeletes.Clear();
        queuedSeasonAdds.Clear();
        queuedSeasonUpdates.Clear();
        queuedSeasonRefreshs.Clear();

        foreach (var ev in queue)
        {

            switch (ev.Item)
            {
                case Movie when ev.EventType is EventType.Remove:
                    queuedMovieDeletes.Add(ev);
                    break;
                case Movie when ev.EventType is EventType.Add:
                    queuedMovieAdds.Add(ev);
                    break;
                case Movie when ev.EventType is EventType.Update:
                    queuedMovieUpdates.Add(ev);
                    break;
                case Movie when ev.EventType is EventType.Refresh:
                    queuedMovieRefreshs.Add(ev);
                    break;
                case Episode when ev.EventType is EventType.Remove:
                    queuedEpisodeDeletes.Add(ev);
                    break;
                case Episode when ev.EventType is EventType.Add:
                    queuedEpisodeAdds.Add(ev);
                    break;
                case Episode when ev.EventType is EventType.Update:
                    queuedEpisodeUpdates.Add(ev);
                    break;
                case Episode when ev.EventType is EventType.Refresh:
                    queuedEpisodeRefreshs.Add(ev);
                    break;
                case Series when ev.EventType is EventType.Remove:
                    queuedShowDeletes.Add(ev);
                    break;
                case Series when ev.EventType is EventType.Add:
                    queuedShowAdds.Add(ev);
                    break;
                case Series when ev.EventType is EventType.Update:
                    queuedShowUpdates.Add(ev);
                    break;
                case Series when ev.EventType is EventType.Refresh:
                    queuedShowRefreshs.Add(ev);
                    break;
                case Season when ev.EventType is EventType.Remove:
                    queuedSeasonDeletes.Add(ev);
                    break;
                case Season when ev.EventType is EventType.Add:
                    queuedSeasonAdds.Add(ev);
                    break;
                case Season when ev.EventType is EventType.Update:
                    queuedSeasonUpdates.Add(ev);
                    break;
                case Season when ev.EventType is EventType.Refresh:
                    queuedSeasonRefreshs.Add(ev);
                    break;
            }

        }

        //await ProcessQueuedMovieEvents(queuedMovieDeletes, EventType.Remove).ConfigureAwait(false);
        await ProcessQueuedMovieEvents(queuedMovieAdds, EventType.Add).ConfigureAwait(false);
        await ProcessQueuedMovieEvents(queuedMovieUpdates, EventType.Update).ConfigureAwait(false);
        await ProcessQueuedMovieEvents(queuedMovieRefreshs, EventType.Refresh).ConfigureAwait(false);

        //await ProcessQueuedEpisodeEvents(queuedEpisodeDeletes, EventType.Remove).ConfigureAwait(false);
        await ProcessQueuedEpisodeEvents(queuedEpisodeAdds, EventType.Add).ConfigureAwait(false);
        await ProcessQueuedEpisodeEvents(queuedEpisodeUpdates, EventType.Update).ConfigureAwait(false);
        await ProcessQueuedEpisodeEvents(queuedEpisodeRefreshs, EventType.Refresh).ConfigureAwait(false);

        //await ProcessQueuedShowEvents(queuedShowDeletes, EventType.Remove).ConfigureAwait(false);
        await ProcessQueuedShowEvents(queuedShowAdds, EventType.Add).ConfigureAwait(false);
        await ProcessQueuedShowEvents(queuedShowUpdates, EventType.Update).ConfigureAwait(false);
        await ProcessQueuedShowEvents(queuedShowRefreshs, EventType.Refresh).ConfigureAwait(false);

        //await ProcessQueuedSeasonEvents(queuedSeasonDeletes, EventType.Remove).ConfigureAwait(false);
        await ProcessQueuedSeasonEvents(queuedSeasonAdds, EventType.Add).ConfigureAwait(false);
        await ProcessQueuedSeasonEvents(queuedSeasonUpdates, EventType.Update).ConfigureAwait(false);
        await ProcessQueuedSeasonEvents(queuedSeasonRefreshs, EventType.Refresh).ConfigureAwait(false);
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

        _logger.LogInformation("Processing {Count} movies with event type {EventType}", events.Count, eventType);

        var movies = events.Select(lev => (Movie)lev.Item)
            .Where(lev => !string.IsNullOrEmpty(lev.Name))
            .ToHashSet();

        try
        {
            // 新增事件也会触发update，不需要处理Add
            // 更新，判断是否有bvid，有的话刷新弹幕文件
            if (eventType == EventType.Add || eventType == EventType.Refresh)
            {
                var queueUpdateMeta = new List<BaseItem>();
                foreach (var item in movies)
                {
                    var providerVal = item.GetProviderId(Plugin.ProviderId) ?? string.Empty;
                    // 视频也支持指定的BV号
                    if (providerVal.StartsWith("BV", StringComparison.CurrentCulture))
                    {
                        var bvid = providerVal;

                        // 下载弹幕xml文件
                        var bytes = await _api.GetDanmaContentAsync(bvid, CancellationToken.None).ConfigureAwait(false);
                        var danmuPath = Path.Combine(item.ContainingFolderPath, item.FileNameWithoutExtension + ".xml");
                        await File.WriteAllBytesAsync(danmuPath, bytes, CancellationToken.None).ConfigureAwait(false);
                    }
                    else
                    {
                        var epId = providerVal.ToLong();
                        if (epId <= 0)
                        {
                            // 搜索查找匹配的视频
                            var searchName = this.GetSearchMovieName(item.Name);
                            var seasonId = await GetMatchSeasonId(item, searchName).ConfigureAwait(false);
                            var season = await _api.GetSeasonAsync(seasonId, CancellationToken.None).ConfigureAwait(false);
                            if (season == null)
                            {
                                _logger.LogInformation("b站没有找到对应视频信息：name={0}", item.Name);
                                continue;
                            }

                            if (season.Episodes.Length > 0)
                            {
                                epId = season.Episodes[0].Id;

                                // 更新epid元数据
                                item.SetProviderId(Plugin.ProviderId, $"{epId}");
                                queueUpdateMeta.Add(item);
                            }
                        }

                        if (epId <= 0)
                        {
                            continue;
                        }

                        // 下载弹幕xml文件
                        var bytes = await _api.GetDanmaContentAsync(epId, CancellationToken.None).ConfigureAwait(false);
                        var danmuPath = Path.Combine(item.ContainingFolderPath, item.FileNameWithoutExtension + ".xml");
                        await File.WriteAllBytesAsync(danmuPath, bytes, CancellationToken.None).ConfigureAwait(false);
                    }
                    // 延迟200毫秒，避免请求太频繁
                    Thread.Sleep(200);
                }

                await ProcessQueuedUpdateMeta(queueUpdateMeta).ConfigureAwait(false);
            }


            // 更新，判断是否有bvid，有的话刷新弹幕文件
            if (eventType == EventType.Update)
            {
                foreach (var item in movies)
                {
                    var providerVal = item.GetProviderId(Plugin.ProviderId) ?? string.Empty;
                    // 视频也支持指定的BV号
                    if (providerVal.StartsWith("BV", StringComparison.CurrentCulture))
                    {
                        var bvid = providerVal;

                        // 下载弹幕xml文件
                        await this.DownloadDanmu(item, bvid).ConfigureAwait(false);
                    }
                    else
                    {
                        var epId = providerVal.ToLong();

                        if (epId <= 0)
                        {
                            this.DeleteOldDanmu(item);
                            continue;
                        }

                        // 下载弹幕xml文件
                        await this.DownloadDanmu(item, epId).ConfigureAwait(false);
                    }

                    // 延迟200毫秒，避免请求太频繁
                    Thread.Sleep(200);
                }
            }


            //// 删除弹幕文件（jellyfin自己会删除）
            //// 修改过名称了怎么办？？？
            //if (eventType == EventType.Remove)
            //{
            //    foreach (var item in movies)
            //    {
            //        // 删除弹幕xml文件
            //        var danmuPath = Path.Combine(item.ContainingFolderPath, item.FileNameWithoutExtension + ".xml");
            //        var fileMeta = _fileSystem.GetFileInfo(danmuPath);
            //        if (fileMeta.Exists)
            //        {
            //            _fileSystem.DeleteFile(danmuPath);
            //        }
            //    }
            //}
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception handled processing queued movie events");
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

        _logger.LogInformation("Processing {Count} shows with event type {EventType}", events.Count, eventType);

        var series = events.Select(lev => (Series)lev.Item)
            .Where(lev => !string.IsNullOrEmpty(lev.Name))
            .ToHashSet();

        try
        {
            if (eventType == EventType.Add || eventType == EventType.Refresh || eventType == EventType.Update)
            {
                foreach (var item in series)
                {
                    var seasons = item.GetSeasons(null, new DtoOptions(false));
                    foreach (var season in seasons)
                    {
                        // 推送刷新season数据
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

        _logger.LogInformation("Processing {Count} seasons with event type {EventType}", events.Count, eventType);

        var seasons = events.Select(lev => (Season)lev.Item)
            .Where(lev => !string.IsNullOrEmpty(lev.Name))
            .ToHashSet();

        try
        {
            if (eventType == EventType.Add || eventType == EventType.Refresh)
            {
                var queueUpdateMeta = new List<BaseItem>();
                foreach (var season in seasons)
                {
                    var series = season.GetParent();
                    var providerVal = season.GetProviderId(Plugin.ProviderId) ?? string.Empty;
                    // 支持视频分片BV号
                    if (providerVal.StartsWith("BV", StringComparison.CurrentCulture))
                    {
                        await ProcessSplitVideo(season, eventType).ConfigureAwait(false);
                    }
                    else
                    {
                        var seasonId = providerVal.ToLong();

                        // 根据名称搜索剧集对应的视频
                        if (seasonId <= 0)
                        {
                            var searchName = GetSearchSeasonName(series.Name, season.IndexNumber ?? 0);
                            seasonId = await GetMatchSeasonId(season, searchName).ConfigureAwait(false);
                            if (seasonId <= 0)
                            {
                                _logger.LogInformation("b站没有找到对应视频信息：name={0}", searchName);
                                continue;
                            }

                            // 更新seasonId元数据
                            season.SetProviderId(Plugin.ProviderId, $"{seasonId}");

                            //await _libraryManager.UpdateItemAsync(season, season.GetParent(), ItemUpdateType.MetadataEdit, CancellationToken.None).ConfigureAwait(false);
                            queueUpdateMeta.Add(season);
                        }

                        if (seasonId > 0)
                        {
                            await ProcessSeasonEpisodes(season, eventType).ConfigureAwait(false);
                        }
                    }
                }

                await ProcessQueuedUpdateMeta(queueUpdateMeta).ConfigureAwait(false);
            }

            if (eventType == EventType.Update)
            {
                foreach (var season in seasons)
                {
                    var providerVal = season.GetProviderId(Plugin.ProviderId) ?? string.Empty;
                    // 支持视频分片BV号
                    if (providerVal.StartsWith("BV", StringComparison.CurrentCulture))
                    {
                        await ProcessSplitVideo(season, eventType).ConfigureAwait(false);
                    }
                    else
                    {
                        var seasonId = providerVal.ToLong();
                        if (seasonId > 0)
                        {
                            await ProcessSeasonEpisodes(season, eventType).ConfigureAwait(false);
                        }
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

        _logger.LogInformation("Processing {Count} episodes with event type {EventType}", events.Count, eventType);

        var episodes = events.Select(lev => (Episode)lev.Item)
            .Where(lev => !string.IsNullOrEmpty(lev.Name))
            .ToHashSet();

        try
        {
            // 判断epid，有的话刷新弹幕文件
            if (eventType == EventType.Update)
            {
                foreach (var item in episodes)
                {
                    var providerVal = item.GetProviderId(Plugin.ProviderId) ?? string.Empty;
                    var epId = providerVal.ToLong();

                    // 新影片，判断是否设置epId，没的话，尝试搜索填充
                    if (epId <= 0)
                    {
                        this.DeleteOldDanmu(item);
                        continue;
                    }

                    // 下载弹幕xml文件
                    await this.DownloadDanmu(item, epId).ConfigureAwait(false);

                    // 延迟200毫秒，避免请求太频繁
                    Thread.Sleep(200);
                }
            }


            // 删除弹幕文件(jellyfin自己会删除)
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception handled processing queued episode events");
        }
    }


    // 分片视频处理
    private async Task ProcessSplitVideo(Season season, EventType eventType)
    {
        try
        {
            var bvid = season.GetProviderId(Plugin.ProviderId) ?? string.Empty;
            if (string.IsNullOrEmpty(bvid) || !bvid.StartsWith("BV", StringComparison.CurrentCulture))
            {
                return;
            }

            // season手动设置了bv号情况
            // 判断剧集数目是否一致，根据集号下载对应的弹幕
            if (eventType == EventType.Update || eventType == EventType.Add || eventType == EventType.Refresh)
            {
                var episodes = season.GetEpisodes(null, new DtoOptions(false));
                var video = await this._api.GetVideoByBvidAsync(bvid, CancellationToken.None).ConfigureAwait(false);
                if (video == null)
                {
                    _logger.LogInformation("获取不到b站视频信息：bvid={0}", bvid);
                    return;
                }

                foreach (var (episode, idx) in episodes.WithIndex())
                {
                    // 分片的集数不规范，采用大于jellyfin集数方式判断.
                    if (video.Pages.Length >= episodes.Count)
                    {
                        var cid = video.Pages[idx].Cid;
                        _logger.LogInformation("视频分片成功匹配. {0} -> index: {1}", episode.Name, idx);

                        // 下载弹幕xml文件
                        await this.DownloadDanmuByCid(episode, cid).ConfigureAwait(false);

                        // 延迟200毫秒，避免请求太频繁
                        Thread.Sleep(200);
                    }
                    else
                    {
                        _logger.LogInformation("刷新弹幕失败, 和b站集数不一致。video: {0} 弹幕数：{1} 集数：{2}", episode.Name, video.Pages.Length, episodes.Count);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception handled ProcessSplitVideo");
        }
    }

    // 每季剧集处理
    private async Task ProcessSeasonEpisodes(Season season, EventType eventType)
    {
        try
        {
            var providerVal = season.GetProviderId(Plugin.ProviderId) ?? string.Empty;
            var seasonId = providerVal.ToLong();
            if (seasonId <= 0)
            {
                return;
            }


            var queueUpdateMeta = new List<BaseItem>();
            var episodes = season.GetEpisodes(null, new DtoOptions(false));
            foreach (var (episode, idx) in episodes.WithIndex())
            {
                var episodeProviderVal = episode.GetProviderId(Plugin.ProviderId) ?? string.Empty;
                var epId = episodeProviderVal.ToLong();
                if (epId > 0)
                {
                    QueueItem(episode, EventType.Update);
                }
                else
                {
                    // seasonId存在，但episode没有epid时，重新匹配获取
                    var seasonData = await _api.GetSeasonAsync(seasonId, CancellationToken.None).ConfigureAwait(false);
                    if (seasonData == null)
                    {
                        return;
                    }

                    var indexNumber = episode.IndexNumber ?? 0;
                    if (indexNumber <= 0)
                    {
                        _logger.LogInformation("匹配失败，缺少集号. [{0}]{1}}", season.Name, episode.Name);
                        continue;
                    }

                    if (indexNumber > seasonData.Episodes.Length)
                    {
                        _logger.LogInformation("匹配失败，集号过大. [{0}]{1}} indexNumber: {2}", season.Name, episode.Name, indexNumber);
                        continue;
                    }

                    if (seasonData.Episodes.Length == episodes.Count)
                    {
                        epId = seasonData.Episodes[idx].Id;
                        _logger.LogInformation("成功匹配. [{0}]{1} -> episode id: {2}", season.Name, episode.Name, epId);

                        // 推送更新epid元数据，（更新元数据后，会触发episode的Update事件从而下载xml)
                        episode.SetProviderId(Plugin.ProviderId, $"{epId}");
                        queueUpdateMeta.Add(episode);
                    }
                }
            }

            await ProcessQueuedUpdateMeta(queueUpdateMeta).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception handled ProcessSplitVideo");
        }
    }

    private void DeleteOldDanmu(BaseItem item)
    {
        // 存在旧弹幕xml文件
        //var oldDanmuPath = Path.Combine(item.ContainingFolderPath, item.FileNameWithoutExtension + ".xml");
        //var fileMeta = _fileSystem.GetFileInfo(oldDanmuPath);
        //if (fileMeta.Exists)
        //{
        //    _fileSystem.DeleteFile(oldDanmuPath);
        //}
    }

    // 根据名称搜索对应的seasonId
    private async Task<long> GetMatchSeasonId(BaseItem item, string searchName)
    {
        try
        {
            var searchResult = await _api.SearchAsync(searchName, CancellationToken.None).ConfigureAwait(false);
            if (searchResult != null && searchResult.Result.Length > 0)
            {
                foreach (var result in searchResult.Result)
                {
                    if ((result.ResultType == "media_ft" || result.ResultType == "media_bangumi") && result.Data.Length > 0)
                    {
                        foreach (var media in result.Data)
                        {
                            var seasonId = media.SeasonId;
                            var title = media.Title;
                            var pubYear = Jellyfin.Plugin.Danmu.Core.Utils.UnixTimeStampToDateTime(media.PublishTime).Year;

                            // 检测标题是否相似（越大越相似）
                            var score = searchName.Distance(title);
                            if (score < 0.7)
                            {
                                _logger.LogInformation("[{0}] 标题差异太大，忽略处理. 搜索词：{1}, score:　{2}", title, searchName, score);
                                continue;
                            }

                            // 检测年份是否一致
                            var itemPubYear = item.ProductionYear ?? 0;
                            if (itemPubYear > 0 && pubYear > 0 && itemPubYear != pubYear)
                            {
                                _logger.LogInformation("[{0}] 发行年份不一致，忽略处理. b站：{1} jellyfin: {2}", title, pubYear, itemPubYear);
                                continue;
                            }

                            _logger.LogInformation("匹配成功. [{0}] seasonId: {1}", title, seasonId);
                            return seasonId;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception handled GetMatchSeasonId. {0}", searchName);
        }

        return 0;
    }

    // 调用UpdateToRepositoryAsync后，但未完成时，会导致GetEpisodes返回缺少正在处理的集数，所以采用统一最后处理
    private async Task ProcessQueuedUpdateMeta(List<BaseItem> queue)
    {
        if (queue.Count <= 0)
        {
            return;
        }

        foreach (var item in queue)
        {
            await item.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, CancellationToken.None).ConfigureAwait(false);
        }
        _logger.LogInformation("更新b站epid到元数据完成。item数：{0}", queue.Count);
    }

    private async Task DownloadDanmu(BaseItem item, long epId)
    {
        // 下载弹幕xml文件
        try
        {
            var bytes = await this._api.GetDanmaContentAsync(epId, CancellationToken.None).ConfigureAwait(false);
            await this.DownloadDanmuInternal(item, bytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception handled download danmu file");
        }
    }

    private async Task DownloadDanmu(BaseItem item, string bvid)
    {
        // 下载弹幕xml文件
        try
        {
            var bytes = await this._api.GetDanmaContentAsync(bvid, CancellationToken.None).ConfigureAwait(false);
            await this.DownloadDanmuInternal(item, bytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception handled download danmu file");
        }
    }

    private async Task DownloadDanmuByCid(BaseItem item, long cid)
    {
        // 下载弹幕xml文件
        try
        {
            var bytes = await this._api.GetDanmaContentByCidAsync(cid, CancellationToken.None).ConfigureAwait(false);
            await this.DownloadDanmuInternal(item, bytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception handled download danmu file");
        }
    }

    private async Task DownloadDanmuInternal(BaseItem item, byte[] bytes)
    {
        // 下载弹幕xml文件
        try
        {
            var danmuPath = Path.Combine(item.ContainingFolderPath, item.FileNameWithoutExtension + ".xml");
            await File.WriteAllBytesAsync(danmuPath, bytes, CancellationToken.None).ConfigureAwait(false);

            var config = Plugin.Instance.Configuration;
            if (config.ToAss && bytes.Length > 0)
            {
                var assConfig = new Danmaku2Ass.Config();
                assConfig.Title = item.Name;
                if (!string.IsNullOrEmpty(config.AssFont.Trim()))
                {
                    assConfig.FontName = config.AssFont;
                }
                if (!string.IsNullOrEmpty(config.AssFontSize.Trim()))
                {
                    assConfig.BaseFontSize = config.AssFontSize.Trim().ToInt();
                }
                if (!string.IsNullOrEmpty(config.AssTextOpacity.Trim()))
                {
                    assConfig.TextOpacity = config.AssTextOpacity.Trim().ToFloat();
                }
                if (!string.IsNullOrEmpty(config.AssLineCount.Trim()))
                {
                    assConfig.LineCount = config.AssLineCount.Trim().ToInt();
                }
                if (!string.IsNullOrEmpty(config.AssSpeed.Trim()))
                {
                    assConfig.TuneDuration = config.AssSpeed.Trim().ToInt() - 8;
                }

                var assPath = Path.Combine(item.ContainingFolderPath, item.FileNameWithoutExtension + ".danmu.ass");
                Bilibili.GetInstance().Create(Encoding.UTF8.GetString(bytes), assConfig, assPath);
            }

            this._logger.LogInformation("弹幕下载成功：name={0}", item.Name);
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Exception handled download danmu file");
        }
    }

    private string GetSearchMovieName(string movieName)
    {
        // 去掉可能存在的季名称
        return Regex.Replace(movieName, @"\s*第.季", "");
    }

    private string GetSearchSeasonName(string seriesName, int seasonIndexNumber)
    {
        var indexName = "";
        switch (seasonIndexNumber)
        {
            case 2:
                indexName = "第二季";
                break;
            case 3:
                indexName = "第三季";
                break;
            case 4:
                indexName = "第四季";
                break;
            case 5:
                indexName = "第五季";
                break;
            case 6:
                indexName = "第六季";
                break;
            case 7:
                indexName = "第七季";
                break;
            case 8:
                indexName = "第八季";
                break;
            case 9:
                indexName = "第九季";
                break;
            default:
                break;
        }

        // 去掉已存在的季名称
        seriesName = Regex.Replace(seriesName, @"\s*第.季", "");
        return $"{seriesName} {indexName}";
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
