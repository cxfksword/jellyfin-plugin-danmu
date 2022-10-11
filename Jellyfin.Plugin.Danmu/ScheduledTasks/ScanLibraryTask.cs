using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.Danmu.Api;
using Jellyfin.Plugin.Danmu.Core;
using Jellyfin.Plugin.Danmu.Model;
using Jellyfin.Plugin.Danmu.Providers;
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
        private readonly BilibiliApi _api;
        private readonly ILogger _logger;
        private readonly LibraryManagerEventsHelper _libraryManagerEventsHelper;


        public string Key => $"{Plugin.Instance.Name}ScanLibrary";

        public string Name => "扫描媒体库匹配弹幕";

        public string Description => $"扫描缺少弹幕的视频，匹配b站元数据后，下载对应弹幕文件。";

        public string Category => Plugin.Instance.Name;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScanLibraryTask"/> class.
        /// </summary>
        /// <param name="loggerFactory">Instance of the <see cref="ILoggerFactory"/> interface.</param>
        /// <param name="api">Instance of the <see cref="BilibiliApi"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        public ScanLibraryTask(ILoggerFactory loggerFactory, BilibiliApi api, ILibraryManager libraryManager, LibraryManagerEventsHelper libraryManagerEventsHelper)
        {
            _logger = loggerFactory.CreateLogger<RefreshDanmuTask>();
            _libraryManager = libraryManager;
            _api = api;
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

            var items = _libraryManager.GetItemList(new InternalItemsQuery
            {
                MediaTypes = new[] { MediaType.Video },
                IncludeItemTypes = new[] { BaseItemKind.Movie, BaseItemKind.Episode }
            }).ToList();

            _logger.LogInformation("Update danmu for {0} videos.", items.Count);


            foreach (var (item, idx) in items.WithIndex())
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress?.Report((double)idx / items.Count * 100);

                var providerVal = item.GetProviderId(Plugin.ProviderId) ?? string.Empty;
                if (!string.IsNullOrEmpty(providerVal))
                {
                    continue;
                }


                // 推送刷新
                switch (item)
                {
                    case Movie:
                        await _libraryManagerEventsHelper.ProcessQueuedMovieEvents(new List<LibraryEvent>() { new LibraryEvent { Item = item, EventType = EventType.Refresh } }, EventType.Refresh).ConfigureAwait(false);
                        break;
                    case Episode:
                        await _libraryManagerEventsHelper.ProcessQueuedEpisodeEvents(new List<LibraryEvent>() { new LibraryEvent { Item = item, EventType = EventType.Refresh } }, EventType.Refresh).ConfigureAwait(false);
                        break;
                    case Series:
                        await _libraryManagerEventsHelper.ProcessQueuedShowEvents(new List<LibraryEvent>() { new LibraryEvent { Item = item, EventType = EventType.Refresh } }, EventType.Refresh).ConfigureAwait(false);
                        break;
                    case Season:
                        await _libraryManagerEventsHelper.ProcessQueuedSeasonEvents(new List<LibraryEvent>() { new LibraryEvent { Item = item, EventType = EventType.Refresh } }, EventType.Refresh).ConfigureAwait(false);
                        break;
                }
            }

            progress?.Report(100);
        }
    }
}
