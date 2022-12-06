using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.Danmu.Api;
using Jellyfin.Plugin.Danmu.Core;
using Jellyfin.Plugin.Danmu.Core.Extensions;
using Jellyfin.Plugin.Danmu.Model;
using Jellyfin.Plugin.Danmu.Scrapers;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Danmu.ScheduledTasks
{
    public class ScanLibraryTask : IScheduledTask
    {
        private readonly ILibraryManager _libraryManager;
        private readonly ScraperFactory _scraperFactory;
        private readonly ILogger _logger;
        private readonly LibraryManagerEventsHelper _libraryManagerEventsHelper;


        public string Key => $"{Plugin.Instance.Name}ScanLibrary";

        public string Name => "扫描媒体库匹配弹幕";

        public string Description => $"扫描缺少弹幕的视频，搜索匹配后，再下载对应弹幕文件。";

        public string Category => Plugin.Instance.Name;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScanLibraryTask"/> class.
        /// </summary>
        /// <param name="loggerFactory">Instance of the <see cref="ILoggerFactory"/> interface.</param>
        /// <param name="api">Instance of the <see cref="BilibiliApi"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        public ScanLibraryTask(ILoggerFactory loggerFactory, ILibraryManager libraryManager, LibraryManagerEventsHelper libraryManagerEventsHelper, ScraperFactory scraperFactory)
        {
            _logger = loggerFactory.CreateLogger<ScanLibraryTask>();
            _libraryManager = libraryManager;
            _scraperFactory = scraperFactory;
            _libraryManagerEventsHelper = libraryManagerEventsHelper;
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return new List<TaskTriggerInfo>();
        }

        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            await Task.Yield();

            progress?.Report(0);

            var scrapers = this._scraperFactory.All();
            var items = _libraryManager.GetItemList(new InternalItemsQuery
            {
                // MediaTypes = new[] { MediaType.Video },
                ExcludeProviderIds = this.GetScraperFilter(scrapers),
                IncludeItemTypes = new[] { BaseItemKind.Movie, BaseItemKind.Season }
            }).ToList();

            _logger.LogInformation("Scan danmu for {0} videos.", items.Count);


            var successCount = 0;
            var failCount = 0;
            foreach (var (item, idx) in items.WithIndex())
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress?.Report((double)idx / items.Count * 100);

                try
                {
                    // 有epid的忽略处理（不需要再匹配）
                    if (this.HasAnyScraperProviderId(scrapers, item))
                    {
                        continue;
                    }

                    // 推送刷新  (season刷新会同时刷新episode，所以不需要再推送episode，而且season是bv号的，只能通过season来刷新)
                    switch (item)
                    {
                        case Movie:
                            await _libraryManagerEventsHelper.ProcessQueuedMovieEvents(new List<LibraryEvent>() { new LibraryEvent { Item = item, EventType = EventType.Add } }, EventType.Add).ConfigureAwait(false);
                            break;
                        case Season:
                            // 搜索匹配season的元数据
                            await _libraryManagerEventsHelper.ProcessQueuedSeasonEvents(new List<LibraryEvent>() { new LibraryEvent { Item = item, EventType = EventType.Add } }, EventType.Add).ConfigureAwait(false);
                            // 下载剧集弹幕
                            await _libraryManagerEventsHelper.ProcessQueuedSeasonEvents(new List<LibraryEvent>() { new LibraryEvent { Item = item, EventType = EventType.Update } }, EventType.Update).ConfigureAwait(false);
                            break;
                            // case Series:
                            //     await _libraryManagerEventsHelper.ProcessQueuedShowEvents(new List<LibraryEvent>() { new LibraryEvent { Item = item, EventType = EventType.Add } }, EventType.Add).ConfigureAwait(false);
                            //     break;

                            // case Episode:
                            //     await _libraryManagerEventsHelper.ProcessQueuedEpisodeEvents(new List<LibraryEvent>() { new LibraryEvent { Item = item, EventType = EventType.Add } }, EventType.Add).ConfigureAwait(false);
                            //     break;
                    }
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

        private bool HasAnyScraperProviderId(ReadOnlyCollection<AbstractScraper> scrapers, BaseItem item)
        {
            foreach (var scraper in scrapers)
            {
                var providerVal = item.GetProviderId(scraper.ProviderId);
                if (!string.IsNullOrEmpty(providerVal))
                {
                    _logger.LogInformation(scraper.Name + " -> " + providerVal);
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
