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
    public class RefreshDanmuTask : AbstractDanmuTask
    {
        private readonly ILibraryManager _libraryManager;
        private readonly ScraperManager _scraperManager;
        private readonly ILogger _logger;
        private readonly LibraryManagerEventsHelper _libraryManagerEventsHelper;


        public override string Key => $"{Plugin.Instance.Name}RefreshDanmu";

        public override string Name => "更新弹幕文件";

        public override string Description => $"根据视频元数据下载最新的弹幕文件。";

        public override string Category => Plugin.Instance.Name;

        /// <summary>
        /// Initializes a new instance of the <see cref="RefreshDanmuTask"/> class.
        /// </summary>
        /// <param name="loggerFactory">Instance of the <see cref="ILoggerFactory"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        public RefreshDanmuTask(ILogManager iLogManager,ILibraryManager libraryManager)
        {
            _logger = iLogManager.getDefaultLogger(GetType().ToString());
            _libraryManager = libraryManager;
            _libraryManagerEventsHelper = SingletonManager.LibraryManagerEventsHelper;
            _scraperManager = SingletonManager.ScraperManager;
            // _libraryManagerEventsHelper = SingletonManager.LibraryManagerEventsHelper;
        }

        public override IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return new List<TaskTriggerInfo>();
            // yield return new TaskTriggerInfo
            // {
            //     Type = TaskTriggerInfo.TriggerWeekly,
            //     DayOfWeek = DayOfWeek.Monday,
            //     TimeOfDayTicks = TimeSpan.FromHours(4).Ticks
            // };
        }

        public override async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            await Task.Yield();

            progress?.Report(0);
            
            _logger.Info("刷新任务开始");

            var scrapers = this._scraperManager.All();
            var items = _libraryManager.GetItemList(new InternalItemsQuery
            {
                // MediaTypes = new[] { MediaType.Video },
                ExcludeProviderIds = this.GetScraperFilter(scrapers),
                IncludeItemTypes = new[] { "Movie", "Episode"}
            }).ToList();

            _logger.LogInformation("Refresh danmu for {0} videos.", items.Count);

            var successCount = 0;
            var failCount = 0;
            for (int idx = 0; idx < items.Count; idx++)
            {
                var item = items[idx];
                cancellationToken.ThrowIfCancellationRequested();
                progress?.Report((double)idx / items.Count * 100);

                try
                {
                    // 没epid元数据的不处理
                    if (!HasAnyScraperProviderId(scrapers, item))
                    {
                        successCount++;
                        continue;
                    }

                    // item所在的媒体库不启用弹幕插件，忽略处理
                    if (_libraryManagerEventsHelper.IsIgnoreItem(item))
                    {
                        continue;
                    }


                    // 推送下载最新的xml (season刷新会同时刷新episode，所以不需要再推送episode，而且season是bv号的，只能通过season来刷新)
                    switch (item)
                    {
                        case Movie movie:
                            await _libraryManagerEventsHelper.ProcessQueuedMovieEvents(new List<LibraryEvent>() { new LibraryEvent { Item = item, EventType = EventType.Update } }, EventType.Update).ConfigureAwait(false);
                            break;
                        case Season season:
                            await _libraryManagerEventsHelper.ProcessQueuedSeasonEvents(new List<LibraryEvent>() { new LibraryEvent { Item = item, EventType = EventType.Update } }, EventType.Update).ConfigureAwait(false);
                            break;
                    }
                    successCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Refresh danmu failed for video {0}: {1}", item.Name, ex.Message);
                    failCount++;
                }
            }

            progress?.Report(100);
            _logger.LogInformation("Exectue task completed. success: {0} fail: {1}", successCount, failCount);
        }
    }
}