using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.Danmu.Model;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Tasks;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;
using Jellyfin.Plugin.Danmu.Core.Extensions;
using Jellyfin.Plugin.Danmu.Scrapers;
using System.Collections.ObjectModel;

namespace Jellyfin.Plugin.Danmu.ScheduledTasks
{
    public class RefreshDanmuTask : IScheduledTask
    {
        private readonly ILibraryManager _libraryManager;
        private readonly ScraperManager _scraperManager;
        private readonly ILogger _logger;
        private readonly LibraryManagerEventsHelper _libraryManagerEventsHelper;


        public string Key => $"{Plugin.Instance.Name}RefreshDanmu";

        public string Name => "更新弹幕文件";

        public string Description => $"根据视频元数据下载最新的弹幕文件。";

        public string Category => Plugin.Instance.Name;

        /// <summary>
        /// Initializes a new instance of the <see cref="RefreshDanmuTask"/> class.
        /// </summary>
        /// <param name="loggerFactory">Instance of the <see cref="ILoggerFactory"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        public RefreshDanmuTask(ILoggerFactory loggerFactory, ILibraryManager libraryManager, LibraryManagerEventsHelper libraryManagerEventsHelper, ScraperManager scraperManager)
        {
            _logger = loggerFactory.CreateLogger<RefreshDanmuTask>();
            _libraryManager = libraryManager;
            _scraperManager = scraperManager;
            _libraryManagerEventsHelper = libraryManagerEventsHelper;
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            yield return new TaskTriggerInfo
            {
                Type = TaskTriggerInfo.TriggerWeekly,
                DayOfWeek = DayOfWeek.Monday,
                TimeOfDayTicks = TimeSpan.FromHours(4).Ticks
            };
        }

        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            await Task.Yield();

            progress?.Report(0);

            var scrapers = this._scraperManager.All();
            var items = _libraryManager.GetItemList(new InternalItemsQuery
            {
                // MediaTypes = new[] { MediaType.Video },
                HasAnyProviderId = GetScraperFilter(scrapers),
                IncludeItemTypes = new[] { BaseItemKind.Movie, BaseItemKind.Season }
            }).ToList();

            _logger.LogInformation("Refresh danmu for {0} videos.", items.Count);

            var successCount = 0;
            var failCount = 0;
            foreach (var (item, idx) in items.WithIndex())
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress?.Report((double)idx / items.Count * 100);

                try
                {
                    // 没epid元数据的不处理
                    if (!this.HasAnyScraperProviderId(scrapers, item))
                    {
                        successCount++;
                        continue;
                    }


                    // 推送下载最新的xml (season刷新会同时刷新episode，所以不需要再推送episode，而且season是bv号的，只能通过season来刷新)
                    switch (item)
                    {
                        case Movie:
                            await _libraryManagerEventsHelper.ProcessQueuedMovieEvents(new List<LibraryEvent>() { new LibraryEvent { Item = item, EventType = EventType.Update } }, EventType.Update).ConfigureAwait(false);
                            break;
                        case Season:
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

        private bool HasAnyScraperProviderId(ReadOnlyCollection<AbstractScraper> scrapers, BaseItem item)
        {
            foreach (var scraper in scrapers)
            {
                var providerVal = item.GetProviderId(scraper.ProviderId);
                if (!string.IsNullOrEmpty(providerVal))
                {
                    return true;
                }
            }

            return false;
        }

        private Dictionary<string, string> GetScraperFilter(ReadOnlyCollection<AbstractScraper> scrapers)
        {
            var filter = new Dictionary<string, string>();
            foreach (var scraper in scrapers)
            {
                filter.Add(scraper.ProviderId, string.Empty);
            }

            return filter;
        }
    }
}
