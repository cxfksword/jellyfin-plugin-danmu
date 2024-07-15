using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Plugin.Danmu.Core.Extensions;
using Emby.Plugin.Danmu.Core.Singleton;
using Emby.Plugin.Danmu.Model;
using Emby.Plugin.Danmu.Scraper;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Tasks;

namespace Emby.Plugin.Danmu.ScheduledTasks
{
    public class ScanLibraryTask : AbstractDanmuTask
    {
        private readonly ILibraryManager _libraryManager;
        private readonly ScraperManager _scraperManager;
        private readonly ILogger _logger;
        private readonly LibraryManagerEventsHelper _libraryManagerEventsHelper;


        public override string Key => $"{Plugin.Instance.Name}ScanLibrary";

        public override string Name => "扫描媒体库匹配弹幕";

        public override string Description => $"扫描缺少弹幕的视频，搜索匹配后，再下载对应弹幕文件。";

        public override string Category => Plugin.Instance.Name;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScanLibraryTask"/> class.
        /// </summary>
        /// <param name="loggerFactory">Instance of the <see cref="ILoggerFactory"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        public ScanLibraryTask(ILogManager logManager, ILibraryManager libraryManager
            // , LibraryManagerEventsHelper libraryManagerEventsHelper
            )
        {
            _logger = logManager.getDefaultLogger(GetType().ToString());
            _libraryManager = libraryManager;
            _libraryManagerEventsHelper = SingletonManager.LibraryManagerEventsHelper;
            // _libraryManagerEventsHelper = SingletonManager.LibraryManagerEventsHelper;
            _scraperManager = SingletonManager.ScraperManager;
            // _libraryManagerEventsHelper = SingletonManager.LibraryManagerEventsHelper;
        }

        // public Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        // {
        //     throw new NotImplementedException();
        // }

        public override IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return new List<TaskTriggerInfo>();
        }

        public override async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            await Task.Yield();
            progress?.Report(0);
            
            _logger.Info("扫描任务开始");

            var scrapers = this._scraperManager.All();
            var items = _libraryManager.GetItemList(new InternalItemsQuery
            {
                ExcludeProviderIds = this.GetScraperFilter(scrapers),
                IncludeItemTypes = new[] { "Movie", "Season"}
            }).ToList();

            var successCount = 0;
            var failCount = 0;
            for (int idx = 0; idx < items.Count; idx++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                BaseItem item = items[idx];
                progress?.Report((double)idx / items.Count * 100);
                try
                {
                    // 有epid的忽略处理（不需要再匹配）
                    if (HasAnyScraperProviderId(scrapers, item))
                    {
                        successCount++;
                        continue;
                    }

                    // item所在的媒体库不启用弹幕插件，忽略处理
                    if (_libraryManagerEventsHelper.IsIgnoreItem(item))
                    {
                        continue;
                    }

                    if (item is Movie)
                    {
                        var movieItem = (Movie)item;
                        await _libraryManagerEventsHelper.ProcessQueuedMovieEvents(new List<LibraryEvent>() { new LibraryEvent { Item = movieItem, EventType = EventType.Add } }, EventType.Add).ConfigureAwait(false);
                    }
                    else if (item is Season)
                    {
                        var seasonItem = (Season)item;
                        // 搜索匹配season的元数据
                        await _libraryManagerEventsHelper.ProcessQueuedSeasonEvents(new List<LibraryEvent>() { new LibraryEvent { Item = seasonItem, EventType = EventType.Add } }, EventType.Add).ConfigureAwait(false);
                        // 下载剧集弹幕
                        await _libraryManagerEventsHelper.ProcessQueuedSeasonEvents(new List<LibraryEvent>() { new LibraryEvent { Item = seasonItem, EventType = EventType.Update } }, EventType.Update).ConfigureAwait(false);
                    } 
                    // else if (item is Episode)
                    // {
                    //     var episodeItem = (Episode)item;
                    //     await _libraryManagerEventsHelper.ProcessQueuedSeasonEvents(new List<LibraryEvent>() { new LibraryEvent { Item = episodeItem, EventType = EventType.Update } }, EventType.Update).ConfigureAwait(false);
                    // }
                    
                    successCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Scan danmu failed for video {0}: {1}", item.Name, ex.Message);
                    failCount++;
                }
            }

            progress?.Report(100);
            _logger.LogInformation("Exectue task completed. success: {0} fail: {1}", successCount, failCount);
        }
    }
}